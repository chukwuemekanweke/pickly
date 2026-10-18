using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.ReconcilePayments;

public sealed class PaymentReconciliationService(
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<Currency> currencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<Delivery> deliveryRepository,
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
            var currency = await currencyRepository.GetByIdAsync(transaction.CurrencyId, cancellationToken)
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
                    currency.CurrencyCode,
                    transaction.PaymentIntent),
                cancellationToken);

            switch (verificationResult.VerificationStatus)
            {
                case PaymentProviderVerificationStatus.Succeeded when transaction.PaymentStatus != Contracts.Payments.PaymentStatus.Succeeded:
                    var verifiedAmount = verificationResult.VerifiedAmount ?? transaction.Amount;
                    var verifiedCurrencyMatches = string.Equals(
                        verificationResult.VerifiedCurrencyCode,
                        currency.CurrencyCode,
                        StringComparison.OrdinalIgnoreCase);
                    if (transaction.PaymentIntent == PaymentIntent.DeliveryPayment &&
                        (verificationResult.VerifiedAmount is null || !verifiedCurrencyMatches))
                    {
                        var attempt = await deliveryPaymentAttemptRepository.FirstOrDefaultAsync(
                            new DeliveryPaymentAttemptByTransactionSpecification(transaction.Id, enableTracking: true),
                            cancellationToken);
                        attempt?.RecordApplicationException("Provider verification did not establish the expected amount and currency.");
                        break;
                    }

                    transaction.ConfirmVerifiedSettlement(
                        verificationResult.ProviderReference,
                        verifiedAmount,
                        transaction.CurrencyId,
                        verificationResult.VerifiedAtUtc ?? now,
                        verificationResult.StatusChangeReason);
                    await eventPublisher.PublishAsync(
                        new SuccessfulPaymentConfirmed
                        {
                            PaymentTransactionId = transaction.Id,
                            MerchantReference = transaction.MerchantReference,
                            PaymentIntent = transaction.PaymentIntent,
                            PaymentProviderId = transaction.PaymentProviderId,
                            Amount = verifiedAmount,
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
                                paymentProvider.ProviderKey,
                                transaction.MerchantReference,
                                Contracts.Payments.PaymentStatus.Succeeded.ToString())));
                    break;
                case PaymentProviderVerificationStatus.Failed when transaction.PaymentStatus is Contracts.Payments.PaymentStatus.PendingInitiation or Contracts.Payments.PaymentStatus.Initiated:
                    transaction.MarkFailed(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason,
                        now);
                    await RecordCurrentDeliveryAttemptFailedAsync(transaction, cancellationToken);
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.ReconciliationFailed,
                        ObservabilityEventProperties.Create(
                            AnonymousActorContext,
                            transaction.StakeholderId,
                            verificationResult.FailureReason,
                            CreateReconciliationProperties(
                                paymentProvider.ProviderKey,
                                transaction.MerchantReference,
                                Contracts.Payments.PaymentStatus.Failed.ToString())));
                    break;
                case PaymentProviderVerificationStatus.Expired when transaction.PaymentStatus is Contracts.Payments.PaymentStatus.PendingInitiation or Contracts.Payments.PaymentStatus.Initiated:
                    transaction.MarkExpired(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason);
                    await RecordCurrentDeliveryAttemptFailedAsync(transaction, cancellationToken);
                    customTelemetryContext.AddCustomEvent(
                        Observability.EventNames.Payments.ReconciliationFailed,
                        ObservabilityEventProperties.Create(
                            AnonymousActorContext,
                            transaction.StakeholderId,
                            verificationResult.FailureReason,
                            CreateReconciliationProperties(
                                paymentProvider.ProviderKey,
                                transaction.MerchantReference,
                                Contracts.Payments.PaymentStatus.Expired.ToString())));
                    break;
                case PaymentProviderVerificationStatus.Processing:
                    break;
            }

            transaction.RecordStatusCheck(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReconcilePaymentsResult(transactions.Count);
    }

    private async Task RecordCurrentDeliveryAttemptFailedAsync(
        PaymentTransaction transaction,
        CancellationToken cancellationToken)
    {
        if (transaction.PaymentIntent != PaymentIntent.DeliveryPayment)
        {
            return;
        }

        var attempt = await deliveryPaymentAttemptRepository.FirstOrDefaultAsync(
            new DeliveryPaymentAttemptByTransactionSpecification(transaction.Id), cancellationToken);
        if (attempt is null)
        {
            return;
        }

        var delivery = await deliveryRepository.GetByIdAsync(attempt.DeliveryId, cancellationToken);
        delivery?.RecordPrepaidPaymentAttemptFailed(transaction.Id);
    }

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
