using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;

public sealed class InitiateDeliveryPaymentHandler(
    IRepository<Delivery> deliveries,
    IRepository<DeliveryPaymentAttempt> deliveryPaymentAttempts,
    IRepository<PaymentTransaction> paymentTransactions,
    IRepository<PaymentProvider> paymentProviders,
    IRepository<PaymentProviderConfiguration> providerConfigurations,
    IRepository<Currency> currencies,
    IRepository<CountryCurrency> countryCurrencies,
    IRepository<Stakeholder> stakeholders,
    IEnumerable<IPaymentProviderService> providerServices,
    IDeliveryPaymentInitiationConsistencyLock consistencyLock,
    IUnitOfWork unitOfWork)
{
    public async Task<InitiateDeliveryPaymentResult> HandleAsync(
        InitiateDeliveryPaymentCommand command,
        CancellationToken cancellationToken)
    {
        var senderId = command.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId == Guid.Empty)
        {
            return new(InitiateDeliveryPaymentStatus.NotAuthenticated);
        }

        var idempotencyKey = NormalizeIdempotencyKey(command.IdempotencyKey);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await consistencyLock.AcquireAsync(command.DeliveryId, cancellationToken);

        var delivery = await deliveries.FirstOrDefaultAsync(
            new DeliveryByOwnerAndIdSpecification(senderId.Value, command.DeliveryId, enableTracking: true),
            cancellationToken);
        if (delivery is null)
        {
            return new(InitiateDeliveryPaymentStatus.NotFound);
        }

        if (delivery.OrderType != DeliveryOrderType.PayBeforeDelivery || delivery.Status != DeliveryStatus.AwaitingPayment)
        {
            return delivery.PaymentStatus == DeliveryPaymentStatus.Paid
                ? new(InitiateDeliveryPaymentStatus.AlreadyPaid)
                : new(InitiateDeliveryPaymentStatus.NotPayable);
        }

        if (delivery.PaymentStatus == DeliveryPaymentStatus.Paid)
        {
            return new(InitiateDeliveryPaymentStatus.AlreadyPaid);
        }

        var attempts = await deliveryPaymentAttempts.ListAsync(
            new DeliveryPaymentAttemptsByDeliverySpecification(delivery.Id, enableTracking: true),
            cancellationToken);
        var attemptTransactions = new List<(DeliveryPaymentAttempt Attempt, PaymentTransaction Transaction)>(attempts.Count);
        foreach (var attempt in attempts)
        {
            var attemptTransaction = await paymentTransactions.GetByIdAsync(attempt.PaymentTransactionId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Payment transaction '{attempt.PaymentTransactionId}' was not found for delivery payment attempt '{attempt.Id}'.");
            attemptTransactions.Add((attempt, attemptTransaction));
        }

        var existing = idempotencyKey is null
            ? null
            : attemptTransactions
                .FirstOrDefault(candidate => candidate.Attempt.IdempotencyKey == idempotencyKey)
                .Transaction;
        existing ??= attemptTransactions
            .Where(candidate => IsOutstanding(candidate.Transaction.PaymentStatus))
            .OrderBy(candidate => OutstandingPriority(candidate.Transaction.PaymentStatus))
            .ThenBy(candidate => candidate.Attempt.CreatedAtUtc)
            .Select(candidate => candidate.Transaction)
            .FirstOrDefault();

        if (existing is not null)
        {
            if (existing.PaymentProviderId != command.PaymentProviderId)
            {
                return new(InitiateDeliveryPaymentStatus.ProviderConflict);
            }

            await transaction.CommitAsync(cancellationToken);
            if (existing.PaymentStatus != PaymentStatus.PendingInitiation)
            {
                var existingProvider = await paymentProviders.GetByIdAsync(existing.PaymentProviderId, cancellationToken);
                return ToResult(existing, existingProvider?.ProviderName ?? string.Empty, true);
            }

            return await InitiateReservedAttemptAsync(existing, cancellationToken, true);
        }

        var stakeholder = await stakeholders.GetByIdAsync(senderId.Value, cancellationToken);
        var currency = await currencies.FirstOrDefaultAsync(
            new ActiveCurrencyByIdSpecification(delivery.Charge.CurrencyId), cancellationToken);
        if (stakeholder is null || currency is null ||
            await countryCurrencies.FirstOrDefaultAsync(
                new CountryCurrencyByCountryAndCurrencySpecification(stakeholder.CountryId, delivery.Charge.CurrencyId),
                cancellationToken) is null)
        {
            return new(InitiateDeliveryPaymentStatus.ProviderUnavailable);
        }

        var provider = await paymentProviders.FirstOrDefaultAsync(
            new ActivePaymentProviderByIdSpecification(command.PaymentProviderId), cancellationToken);
        if (provider is null ||
            await providerConfigurations.FirstOrDefaultAsync(
                new EnabledPaymentProviderConfigurationSpecification(
                    provider.Id, delivery.Charge.CurrencyId, PaymentIntent.DeliveryPayment),
                cancellationToken) is null ||
            providerServices.All(service => !string.Equals(service.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase)))
        {
            return new(InitiateDeliveryPaymentStatus.ProviderUnavailable);
        }

        var paymentTransaction = PaymentTransaction.Create(
            $"delivery_{Guid.CreateVersion7():N}",
            PaymentIntent.DeliveryPayment,
            provider.Id,
            delivery.Charge.Amount,
            delivery.Charge.CurrencyId,
            stakeholder.CountryId,
            stakeholder.AppUserId,
            stakeholder.Id,
            stakeholder.TenantId);
        var paymentAttempt = DeliveryPaymentAttempt.Create(delivery.Id, paymentTransaction.Id, idempotencyKey);
        await paymentTransactions.AddAsync(paymentTransaction, cancellationToken);
        await deliveryPaymentAttempts.AddAsync(paymentAttempt, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await InitiateReservedAttemptAsync(paymentTransaction, cancellationToken, false);
    }

    private async Task<InitiateDeliveryPaymentResult> InitiateReservedAttemptAsync(
        PaymentTransaction paymentTransaction,
        CancellationToken cancellationToken,
        bool isReplay)
    {
        var provider = await paymentProviders.GetByIdAsync(paymentTransaction.PaymentProviderId, cancellationToken)
            ?? throw new InvalidOperationException("The reserved payment provider no longer exists.");
        var currency = await currencies.GetByIdAsync(paymentTransaction.CurrencyId, cancellationToken)
            ?? throw new InvalidOperationException("The reserved payment currency no longer exists.");
        var service = providerServices.SingleOrDefault(candidate =>
            string.Equals(candidate.ProviderKey, provider.ProviderKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new PaymentProviderResolutionException($"No payment provider service is registered for '{provider.ProviderKey}'.");

        try
        {
            var result = await service.InitiatePaymentAsync(
                new PaymentProviderInitiationRequest(
                    paymentTransaction.MerchantReference,
                    paymentTransaction.Amount,
                    currency.CurrencyCode,
                    paymentTransaction.PaymentIntent,
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
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToResult(paymentTransaction, provider.ProviderName, isReplay);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new(InitiateDeliveryPaymentStatus.ProviderUnavailable, paymentTransaction.Id, paymentTransaction.MerchantReference);
        }
        catch (HttpRequestException)
        {
            return new(InitiateDeliveryPaymentStatus.ProviderUnavailable, paymentTransaction.Id, paymentTransaction.MerchantReference);
        }
    }

    private static InitiateDeliveryPaymentResult ToResult(
        PaymentTransaction transaction,
        string providerName,
        bool isReplay) =>
        new(
            InitiateDeliveryPaymentStatus.Success,
            transaction.Id,
            transaction.MerchantReference,
            transaction.PaymentStatus,
            transaction.PaymentProviderId,
            providerName,
            transaction.ExpiresAtUtc,
            transaction.PaymentMethodType,
            transaction.ProviderPayloadMetadata,
            isReplay);

    private static string? NormalizeIdempotencyKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= 200
            ? normalized
            : throw new ArgumentException("Idempotency-Key cannot exceed 200 characters.", nameof(value));
    }

    private static bool IsOutstanding(PaymentStatus status) =>
        status is PaymentStatus.PendingInitiation or
            PaymentStatus.Initiated or
            PaymentStatus.Succeeded or
            PaymentStatus.Unresolved;

    private static int OutstandingPriority(PaymentStatus status) => status switch
    {
        PaymentStatus.Unresolved => 1,
        PaymentStatus.Succeeded => 2,
        PaymentStatus.Initiated => 3,
        PaymentStatus.PendingInitiation => 4,
        _ => int.MaxValue
    };
}
