using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;

public sealed class InitiateDeliveryPaymentHandler(
    IRepository<Delivery> deliveries,
    IRepository<DeliveryPaymentAttempt> deliveryPaymentAttempts,
    IDeliveryPaymentAttemptReadModelRepository paymentAttempts,
    IRepository<PaymentTransaction> paymentTransactions,
    IRepository<PaymentProvider> paymentProviders,
    IRepository<PaymentProviderConfiguration> providerConfigurations,
    IRepository<Currency> currencies,
    IRepository<CountryCurrency> countryCurrencies,
    IRepository<Stakeholder> stakeholders,
    IStakeholderReadModelRepository stakeholderReadModels,
    IEnumerable<IPaymentProviderService> providerServices,
    IDeliveryPaymentInitiationConsistencyLock consistencyLock,
    ICommandSender commandSender,
    IOptions<DeliveryNotificationOptions> notificationOptions,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
{
    private const int MaximumIdempotencyKeyLength = 200;

    private const string MerchantReferencePrefix = "delivery_";
    private const string AmountFormat = "N2";
    private const string DeliveryPaymentPathFormat = "deliveries/{0}/payment";
    private const string DisplayDateTimeFormat = "dd MMM yyyy, h:mm tt 'WAT'";
    private const string DisplayCultureName = "en-NG";

    private const string FirstNameReplacement = "FirstName";
    private const string IllustrationUrlReplacement = "IllustrationUrl";
    private const string TrackingIdReplacement = "TrackingId";
    private const string AmountReplacement = "Amount";
    private const string CurrencyReplacement = "Currency";
    private const string PaymentReferenceReplacement = "PaymentReference";
    private const string PaymentProviderReplacement = "PaymentProvider";
    private const string PaymentMethodReplacement = "PaymentMethod";
    private const string PaymentInstructionsReplacement = "PaymentInstructions";
    private const string PaymentExpiryReplacement = "PaymentExpiry";
    private const string ActionUrlReplacement = "ActionUrl";

    private const string BankNameInstruction = "bankName";
    private const string AccountNameInstruction = "accountName";
    private const string AccountNumberInstruction = "accountNumber";
    private const string PaymentLinkInstruction = "paymentLink";
    private const string BankLabel = "Bank";
    private const string AccountNameLabel = "Account name";
    private const string AccountNumberLabel = "Account number";
    private const string PaymentLinkLabel = "Secure payment link";

    private const string MissingPaymentAssociationMessage =
        "Payment transaction '{0}' is no longer associated with the delivery.";
    private const string MissingReservedPaymentTransactionMessage =
        "Reserved payment transaction '{0}' was not found.";
    private const string MissingDeliveryMessage =
        "Delivery '{0}' was not found while preparing payment instructions.";
    private const string MissingPayingStakeholderMessage =
        "Paying stakeholder '{0}' was not found.";
    private const string IdempotencyKeyTooLongMessage =
        "Idempotency-Key cannot exceed 200 characters.";
    private const string MissingProviderDeadlineMessage = "No provider deadline supplied";
    private const string DefaultPaymentInstructionsMessage =
        "Open the delivery payment page to continue with the provider's instructions.";

    public async Task<InitiateDeliveryPaymentResult> HandleAsync(
        InitiateDeliveryPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var senderId = command.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId == Guid.Empty)
        {
            return new(InitiateDeliveryPaymentStatus.NotAuthenticated);
        }

        var decision = await ReserveOrResolveAsync(
            command,
            senderId.Value,
            NormalizeIdempotencyKey(command.IdempotencyKey),
            cancellationToken);
        if (decision.Result is not null)
        {
            return decision.Result;
        }

        return await InitiateReservedAttemptAsync(
            command.DeliveryId,
            decision.PaymentTransactionId!.Value,
            decision.IsReplay,
            cancellationToken);
    }

    private async Task<InitiationDecision> ReserveOrResolveAsync(
        InitiateDeliveryPaymentCommand command,
        Guid senderId,
        string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await consistencyLock.AcquireAsync(command.DeliveryId, cancellationToken);

        var delivery = await deliveries.FirstOrDefaultAsync(
            new DeliveryByOwnerAndIdSpecification(senderId, command.DeliveryId, enableTracking: true),
            cancellationToken);
        if (delivery is null)
        {
            return InitiationDecision.FromResult(new(InitiateDeliveryPaymentStatus.NotFound));
        }

        if (delivery.OrderType != DeliveryOrderType.PayBeforeDelivery || delivery.Status != DeliveryStatus.AwaitingPayment)
        {
            return InitiationDecision.FromResult(delivery.PaymentStatus == DeliveryPaymentStatus.Paid
                ? new(InitiateDeliveryPaymentStatus.AlreadyPaid)
                : new(InitiateDeliveryPaymentStatus.NotPayable));
        }

        if (delivery.PaymentStatus == DeliveryPaymentStatus.Paid)
        {
            return InitiationDecision.FromResult(new(InitiateDeliveryPaymentStatus.AlreadyPaid));
        }

        var attempts = await paymentAttempts.ListForDecisionAsync(delivery.Id, cancellationToken);

        // An unresolved financial outcome always wins, including over an exact idempotency-key replay.
        var unresolved = attempts.FirstOrDefault(attempt => attempt.PaymentStatus == PaymentStatus.Unresolved);
        if (unresolved is not null)
        {
            return InitiationDecision.FromResult(ToResult(unresolved, true));
        }

        var exactAttempt = idempotencyKey is null
            ? null
            : attempts.FirstOrDefault(attempt => attempt.IdempotencyKey == idempotencyKey);
        if (exactAttempt is not null)
        {
            return exactAttempt.PaymentStatus == PaymentStatus.PendingInitiation
                ? InitiationDecision.Initiate(exactAttempt.PaymentTransactionId, true)
                : InitiationDecision.FromResult(ToResult(exactAttempt, true));
        }

        // Explicit business order: settled-but-unapplied, provider-initialized, then reserved.
        var outstanding = attempts.FirstOrDefault(attempt => attempt.PaymentStatus == PaymentStatus.Succeeded)
            ?? attempts.FirstOrDefault(attempt => attempt.PaymentStatus == PaymentStatus.Initiated)
            ?? attempts.FirstOrDefault(attempt => attempt.PaymentStatus == PaymentStatus.PendingInitiation);
        if (outstanding is not null)
        {
            if (outstanding.PaymentProviderId != command.PaymentProviderId)
            {
                return InitiationDecision.FromResult(new(InitiateDeliveryPaymentStatus.ProviderConflict));
            }

            return outstanding.PaymentStatus == PaymentStatus.PendingInitiation
                ? InitiationDecision.Initiate(outstanding.PaymentTransactionId, true)
                : InitiationDecision.FromResult(ToResult(outstanding, true));
        }

        var stakeholder = await stakeholders.GetByIdAsync(senderId, cancellationToken);
        var currency = await currencies.FirstOrDefaultAsync(
            new ActiveCurrencyByIdSpecification(delivery.Charge.CurrencyId), cancellationToken);
        if (stakeholder is null || currency is null ||
            await countryCurrencies.FirstOrDefaultAsync(
                new CountryCurrencyByCountryAndCurrencySpecification(stakeholder.CountryId, delivery.Charge.CurrencyId),
                cancellationToken) is null)
        {
            return InitiationDecision.FromResult(new(InitiateDeliveryPaymentStatus.ProviderUnavailable));
        }

        var provider = await paymentProviders.FirstOrDefaultAsync(
            new ActivePaymentProviderByIdSpecification(command.PaymentProviderId), cancellationToken);
        if (provider is null ||
            await providerConfigurations.FirstOrDefaultAsync(
                new EnabledPaymentProviderConfigurationSpecification(
                    provider.Id, delivery.Charge.CurrencyId, PaymentIntent.DeliveryPayment),
                cancellationToken) is null ||
            providerServices.All(service => !string.Equals(
                service.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase)))
        {
            return InitiationDecision.FromResult(new(InitiateDeliveryPaymentStatus.ProviderUnavailable));
        }

        var paymentTransaction = PaymentTransaction.Create(
            $"{MerchantReferencePrefix}{Guid.CreateVersion7():N}",
            PaymentIntent.DeliveryPayment,
            provider.Id,
            delivery.Charge.Amount,
            delivery.Charge.CurrencyId,
            stakeholder.CountryId,
            stakeholder.AppUserId,
            stakeholder.Id,
            stakeholder.TenantId);
        await paymentTransactions.AddAsync(paymentTransaction, cancellationToken);
        await deliveryPaymentAttempts.AddAsync(
            DeliveryPaymentAttempt.Create(delivery.Id, paymentTransaction.Id, idempotencyKey), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return InitiationDecision.Initiate(paymentTransaction.Id, false);
    }

    private async Task<InitiateDeliveryPaymentResult> InitiateReservedAttemptAsync(
        Guid deliveryId,
        Guid paymentTransactionId,
        bool isReplay,
        CancellationToken cancellationToken)
    {
        // Keep the delivery lock through provider I/O and the local state transition. A competing
        // caller re-reads after this transaction commits and therefore cannot initialize twice.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await consistencyLock.AcquireAsync(deliveryId, cancellationToken);

        var snapshot = await paymentAttempts.GetForDecisionAsync(paymentTransactionId, cancellationToken)
            ?? throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                MissingPaymentAssociationMessage,
                paymentTransactionId));
        if (snapshot.PaymentStatus != PaymentStatus.PendingInitiation)
        {
            return ToResult(snapshot, true);
        }

        var paymentTransaction = await paymentTransactions.GetByIdAsync(paymentTransactionId, cancellationToken)
            ?? throw new InvalidOperationException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                MissingReservedPaymentTransactionMessage,
                paymentTransactionId));
        var service = providerServices.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, snapshot.PaymentProviderKey, StringComparison.OrdinalIgnoreCase));
        if (service is null)
        {
            return ProviderUnavailable(snapshot);
        }

        try
        {
            var result = await service.InitiatePaymentAsync(
                new PaymentProviderInitiationRequest(
                    snapshot.MerchantReference,
                    snapshot.Amount,
                    snapshot.CurrencyCode,
                    PaymentIntent.DeliveryPayment,
                    paymentTransaction.StakeholderId,
                    paymentTransaction.TenantId,
                    paymentTransaction.CountryId),
                cancellationToken);
            paymentTransaction.MarkInitiated(
                result.ProviderReference,
                result.InstructionFields.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
                result.ExpiresAtUtc,
                KnownPaymentTransactionChangeReasons.PaymentInitiated);
            paymentTransaction.SetPaymentMethodType(result.PaymentMethodType);
            var requestedAt = timeProvider.GetUtcNow();
            if (HasActionableInstructions(result.InstructionFields) &&
                (result.ExpiresAtUtc is null || result.ExpiresAtUtc > requestedAt))
            {
                var delivery = await deliveries.GetByIdAsync(deliveryId, cancellationToken)
                    ?? throw new InvalidOperationException(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        MissingDeliveryMessage,
                        deliveryId));
                var payer = await stakeholderReadModels.GetByStakeholderIdAsync(
                    paymentTransaction.StakeholderId,
                    cancellationToken)
                    ?? throw new InvalidOperationException(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        MissingPayingStakeholderMessage,
                        paymentTransaction.StakeholderId));
                await commandSender.SendAsync(
                    new SendNotificationCommand(
                        payer.TenantId,
                        payer.CountryId,
                        NotificationType.PaymentInitiated,
                        NotificationMedium.Email,
                        new EmailNotificationContent(
                            payer.EmailAddress,
                            new Dictionary<string, string>
                            {
                                [FirstNameReplacement] = payer.FirstName,
                                [IllustrationUrlReplacement] = NotificationIllustrationUrls.PaymentInitiated,
                                [TrackingIdReplacement] = delivery.TrackingId,
                                [AmountReplacement] = paymentTransaction.Amount.ToString(
                                    AmountFormat,
                                    System.Globalization.CultureInfo.InvariantCulture),
                                [CurrencyReplacement] = snapshot.CurrencyCode,
                                [PaymentReferenceReplacement] = paymentTransaction.MerchantReference,
                                [PaymentProviderReplacement] = snapshot.PaymentProviderName,
                                [PaymentMethodReplacement] = paymentTransaction.PaymentMethodType.ToString(),
                                [PaymentInstructionsReplacement] = FormatPaymentInstructions(result.InstructionFields),
                                [PaymentExpiryReplacement] = FormatExpiry(result.ExpiresAtUtc),
                                [ActionUrlReplacement] = PaymentActionUrl(delivery, result.InstructionFields)
                            }))
                    {
                        MessageId = NotificationMessageIdentity.Create(
                            paymentTransaction.Id,
                            NotificationType.PaymentInitiated,
                            payer.StakeholderId),
                        RequestedAt = requestedAt,
                        StakeholderId = payer.StakeholderId,
                        Language = payer.Language,
                        ExpiresAtUtc = result.ExpiresAtUtc
                    },
                    cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResult(paymentTransaction, snapshot.PaymentProviderName, isReplay);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProviderUnavailable(snapshot);
        }
        catch (HttpRequestException)
        {
            return ProviderUnavailable(snapshot);
        }
    }

    private static InitiateDeliveryPaymentResult ProviderUnavailable(DeliveryPaymentAttemptReadModel attempt) =>
        new(InitiateDeliveryPaymentStatus.ProviderUnavailable, attempt.PaymentTransactionId, attempt.MerchantReference);

    private static InitiateDeliveryPaymentResult ToResult(
        DeliveryPaymentAttemptReadModel attempt,
        bool isReplay) =>
        new(
            ResultStatus(attempt.PaymentStatus),
            attempt.PaymentTransactionId,
            attempt.MerchantReference,
            attempt.PaymentStatus,
            attempt.PaymentProviderId,
            attempt.PaymentProviderName,
            attempt.ExpiresAtUtc,
            attempt.PaymentMethodType,
            attempt.PaymentStatus == PaymentStatus.Initiated ? attempt.ProviderPayloadMetadata : null,
            isReplay);

    private static InitiateDeliveryPaymentResult ToResult(
        PaymentTransaction transaction,
        string providerName,
        bool isReplay) =>
        new(
            ResultStatus(transaction.PaymentStatus),
            transaction.Id,
            transaction.MerchantReference,
            transaction.PaymentStatus,
            transaction.PaymentProviderId,
            providerName,
            transaction.ExpiresAtUtc,
            transaction.PaymentMethodType,
            transaction.PaymentStatus == PaymentStatus.Initiated ? transaction.ProviderPayloadMetadata : null,
            isReplay);

    private static InitiateDeliveryPaymentStatus ResultStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Failed => InitiateDeliveryPaymentStatus.PaymentFailed,
        PaymentStatus.Expired => InitiateDeliveryPaymentStatus.PaymentExpired,
        PaymentStatus.Cancelled => InitiateDeliveryPaymentStatus.PaymentCancelled,
        PaymentStatus.Unresolved => InitiateDeliveryPaymentStatus.PaymentRequiresReview,
        _ => InitiateDeliveryPaymentStatus.Success
    };

    private static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= MaximumIdempotencyKeyLength
            ? normalized
            : throw new ArgumentException(IdempotencyKeyTooLongMessage, nameof(value));
    }

    private string PaymentActionUrl(
        Delivery delivery,
        IReadOnlyDictionary<string, string> instructions)
    {
        if (TryGetInstruction(instructions, PaymentLinkInstruction, out var paymentLink) &&
            Uri.TryCreate(paymentLink, UriKind.Absolute, out var paymentUri) &&
            paymentUri.Scheme == Uri.UriSchemeHttps)
        {
            return paymentUri.AbsoluteUri;
        }

        return new Uri(
            new Uri(notificationOptions.Value.ApplicationBaseUrl.TrimEnd('/') + '/'),
            string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                DeliveryPaymentPathFormat,
                Uri.EscapeDataString(delivery.TrackingId))).AbsoluteUri;
    }

    private string FormatExpiry(DateTimeOffset? expiresAtUtc)
    {
        if (!expiresAtUtc.HasValue)
        {
            return MissingProviderDeadlineMessage;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(notificationOptions.Value.DisplayTimeZoneId);
        return TimeZoneInfo.ConvertTime(expiresAtUtc.Value, timeZone)
            .ToString(
                DisplayDateTimeFormat,
                System.Globalization.CultureInfo.GetCultureInfo(DisplayCultureName));
    }

    private static string FormatPaymentInstructions(IReadOnlyDictionary<string, string> instructions)
    {
        var lines = new List<string>();
        AddInstruction(lines, instructions, BankNameInstruction, BankLabel);
        AddInstruction(lines, instructions, AccountNameInstruction, AccountNameLabel);
        AddInstruction(lines, instructions, AccountNumberInstruction, AccountNumberLabel);
        AddInstruction(lines, instructions, PaymentLinkInstruction, PaymentLinkLabel);
        return lines.Count == 0
            ? DefaultPaymentInstructionsMessage
            : string.Join(Environment.NewLine, lines);
    }

    private static bool HasActionableInstructions(IReadOnlyDictionary<string, string> instructions) =>
        TryGetInstruction(instructions, AccountNumberInstruction, out _) ||
        (TryGetInstruction(instructions, PaymentLinkInstruction, out var paymentLink) &&
         Uri.TryCreate(paymentLink, UriKind.Absolute, out var paymentUri) &&
         paymentUri.Scheme == Uri.UriSchemeHttps);

    private static void AddInstruction(
        List<string> lines,
        IReadOnlyDictionary<string, string> instructions,
        string key,
        string label)
    {
        if (TryGetInstruction(instructions, key, out var value))
        {
            lines.Add($"{label}: {value}");
        }
    }

    private static bool TryGetInstruction(
        IReadOnlyDictionary<string, string> instructions,
        string key,
        out string value)
    {
        var match = instructions.FirstOrDefault(entry =>
            string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase));
        value = match.Value?.Trim() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private sealed record InitiationDecision(
        InitiateDeliveryPaymentResult? Result,
        Guid? PaymentTransactionId,
        bool IsReplay)
    {
        public static InitiationDecision FromResult(InitiateDeliveryPaymentResult result) => new(result, null, false);

        public static InitiationDecision Initiate(Guid paymentTransactionId, bool isReplay) =>
            new(null, paymentTransactionId, isReplay);
    }
}
