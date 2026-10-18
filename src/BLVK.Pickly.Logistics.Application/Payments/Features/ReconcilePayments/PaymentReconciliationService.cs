using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.ReconcilePayments;

public sealed class PaymentReconciliationService(
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<Currency> currencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<DeliveryPaymentAttempt> deliveryPaymentAttemptRepository,
    IEnumerable<IPaymentProviderService> paymentProviderServices,
    IEventPublisher eventPublisher,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private static readonly ActorContext AnonymousActorContext = new(null, null, string.Empty, string.Empty);

    public async Task<ReconcilePaymentsResult> HandleAsync(
        DateTimeOffset oldestEligibleCreatedAtUtc,
        DateTimeOffset staleThresholdUtc,
        DateTimeOffset nextCheckThresholdUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var transactions = await paymentTransactionRepository.ListAsync(
            new PendingPaymentReconciliationSpecification(
                oldestEligibleCreatedAtUtc,
                staleThresholdUtc,
                nextCheckThresholdUtc,
                batchSize),
            cancellationToken);
        if (transactions.Count == 0)
        {
            return new ReconcilePaymentsResult(0);
        }

        var now = timeProvider.GetUtcNow();
        foreach (var transaction in transactions)
        {
            var expectedCurrency = await currencyRepository.GetByIdAsync(transaction.CurrencyId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Currency '{transaction.CurrencyId}' was not found for payment transaction '{transaction.Id}'.");
            var paymentProvider = await paymentProviderRepository.GetByIdAsync(transaction.PaymentProviderId, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Payment provider '{transaction.PaymentProviderId}' was not found for payment transaction '{transaction.Id}'.");
            var paymentProviderService = paymentProviderServices.SingleOrDefault(service =>
                    string.Equals(service.ProviderKey, paymentProvider.ProviderKey, StringComparison.OrdinalIgnoreCase))
                ?? throw new PaymentProviderResolutionException(
                    $"No payment provider service is registered for '{paymentProvider.ProviderKey}'.");

            var verificationResult = await paymentProviderService.VerifyPaymentAsync(
                new PaymentProviderVerificationRequest(
                    transaction.MerchantReference,
                    transaction.ProviderReference,
                    transaction.Amount,
                    expectedCurrency.CurrencyCode,
                    transaction.PaymentIntent),
                cancellationToken);

            switch (verificationResult.VerificationStatus)
            {
                case PaymentProviderVerificationStatus.Succeeded when transaction.PaymentStatus != PaymentStatus.Succeeded:
                    await ReconcileReportedSettlementAsync(
                        transaction,
                        expectedCurrency,
                        paymentProvider.ProviderKey,
                        verificationResult,
                        now,
                        cancellationToken);
                    break;
                case PaymentProviderVerificationStatus.Failed when transaction.PaymentStatus is PaymentStatus.PendingInitiation or PaymentStatus.Initiated:
                    transaction.MarkFailed(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason,
                        now);
                    RecordTelemetry(
                        transaction,
                        paymentProvider.ProviderKey,
                        verificationResult.FailureReason,
                        PaymentStatus.Failed);
                    break;
                case PaymentProviderVerificationStatus.Expired when transaction.PaymentStatus is PaymentStatus.PendingInitiation or PaymentStatus.Initiated:
                    transaction.MarkExpired(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason);
                    RecordTelemetry(
                        transaction,
                        paymentProvider.ProviderKey,
                        verificationResult.FailureReason,
                        PaymentStatus.Expired);
                    break;
                case PaymentProviderVerificationStatus.Processing:
                    break;
            }

            transaction.RecordStatusCheck(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReconcilePaymentsResult(transactions.Count);
    }

    private async Task ReconcileReportedSettlementAsync(
        PaymentTransaction transaction,
        Currency expectedCurrency,
        string providerKey,
        PaymentProviderVerificationResult verificationResult,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var verifiedCurrencyCode = NormalizeCurrencyCode(verificationResult.VerifiedCurrencyCode);
        var amountMatches = verificationResult.VerifiedAmount == transaction.Amount;
        var currencyMatches = string.Equals(
            verifiedCurrencyCode,
            expectedCurrency.CurrencyCode,
            StringComparison.OrdinalIgnoreCase);

        if (!amountMatches || !currencyMatches)
        {
            var verifiedCurrencyId = await ResolveVerifiedCurrencyIdAsync(verifiedCurrencyCode, cancellationToken);
            var discrepancy = CreateDiscrepancyReason(
                transaction,
                expectedCurrency.CurrencyCode,
                verificationResult.VerifiedAmount,
                verifiedCurrencyCode);
            transaction.MarkUnresolved(
                verificationResult.ProviderReference,
                verificationResult.VerifiedAmount,
                verifiedCurrencyId,
                verifiedCurrencyCode,
                verificationResult.VerifiedAtUtc,
                discrepancy,
                KnownPaymentTransactionChangeReasons.ReconciliationUnresolvedSettlement);
            await RecordDeliveryApplicationExceptionAsync(transaction, discrepancy, cancellationToken);
            RecordTelemetry(transaction, providerKey, discrepancy, PaymentStatus.Unresolved);
            return;
        }

        var verifiedAtUtc = verificationResult.VerifiedAtUtc ?? now;
        if (transaction.PaymentStatus is PaymentStatus.Failed or PaymentStatus.Expired)
        {
            transaction.ConfirmLateVerifiedSettlement(
                verificationResult.ProviderReference,
                verificationResult.VerifiedAmount!.Value,
                transaction.CurrencyId,
                verifiedAtUtc,
                verificationResult.StatusChangeReason,
                verifiedCurrencyCode);
        }
        else
        {
            transaction.ConfirmVerifiedSettlement(
                verificationResult.ProviderReference,
                verificationResult.VerifiedAmount!.Value,
                transaction.CurrencyId,
                verifiedAtUtc,
                verificationResult.StatusChangeReason,
                verifiedCurrencyCode);
        }

        await eventPublisher.PublishAsync(
            new SuccessfulPaymentConfirmed
            {
                PaymentTransactionId = transaction.Id,
                MerchantReference = transaction.MerchantReference,
                PaymentIntent = transaction.PaymentIntent,
                PaymentProviderId = transaction.PaymentProviderId,
                Amount = transaction.VerifiedAmount.Value,
                CurrencyId = transaction.VerifiedCurrencyId!.Value,
                StakeholderId = transaction.StakeholderId,
                TenantId = transaction.TenantId,
                FlowId = string.Empty,
                OccuredAt = now
            },
            cancellationToken);

        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Payments.ReconciliationConfirmed,
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                transaction.StakeholderId,
                additionalProperties: CreateReconciliationProperties(
                    providerKey,
                    transaction.MerchantReference,
                    PaymentStatus.Succeeded.ToString())));
    }

    private async Task<Guid?> ResolveVerifiedCurrencyIdAsync(
        string? verifiedCurrencyCode,
        CancellationToken cancellationToken)
    {
        if (verifiedCurrencyCode is null)
        {
            return null;
        }

        var currency = await currencyRepository.FirstOrDefaultAsync(
            new CurrencyByCodeSpecification(verifiedCurrencyCode), cancellationToken);
        return currency?.Id;
    }

    private async Task RecordDeliveryApplicationExceptionAsync(
        PaymentTransaction transaction,
        string reason,
        CancellationToken cancellationToken)
    {
        if (transaction.PaymentIntent != PaymentIntent.DeliveryPayment)
        {
            return;
        }

        var attempt = await deliveryPaymentAttemptRepository.FirstOrDefaultAsync(
            new DeliveryPaymentAttemptByTransactionSpecification(transaction.Id, enableTracking: true),
            cancellationToken);
        attempt?.RecordApplicationException(reason);
    }

    private void RecordTelemetry(
        PaymentTransaction transaction,
        string providerKey,
        string? failureReason,
        PaymentStatus terminalState) =>
        customTelemetryContext.AddCustomEvent(
            terminalState == PaymentStatus.Succeeded
                ? Observability.EventNames.Payments.ReconciliationConfirmed
                : Observability.EventNames.Payments.ReconciliationFailed,
            ObservabilityEventProperties.Create(
                AnonymousActorContext,
                transaction.StakeholderId,
                failureReason,
                CreateReconciliationProperties(
                    providerKey,
                    transaction.MerchantReference,
                    terminalState.ToString())));

    private static string CreateDiscrepancyReason(
        PaymentTransaction transaction,
        string expectedCurrencyCode,
        decimal? verifiedAmount,
        string? verifiedCurrencyCode)
    {
        var actualAmount = verifiedAmount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing";
        var actualCurrency = verifiedCurrencyCode ?? "missing";
        return $"Provider settlement evidence did not match the request. Expected {transaction.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)} {expectedCurrencyCode}; verified {actualAmount} {actualCurrency}.";
    }

    private static string? NormalizeCurrencyCode(string? currencyCode) =>
        string.IsNullOrWhiteSpace(currencyCode) ? null : currencyCode.Trim().ToUpperInvariant();

    private static Dictionary<string, string> CreateReconciliationProperties(
        string provider,
        string paymentReference,
        string terminalState) =>
        new()
        {
            [Observability.PropertyNames.Payments.Provider] = provider,
            [Observability.PropertyNames.Payments.PaymentReference] = paymentReference,
            [Observability.PropertyNames.Payments.TerminalState] = terminalState
        };
}
