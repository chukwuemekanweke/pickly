using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.ReconcilePayments;

public sealed class PaymentReconciliationService(
    IRepository<PaymentTransaction> paymentTransactionRepository,
    IRepository<Currency> currencyRepository,
    IRepository<PaymentProvider> paymentProviderRepository,
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
                    transaction.MarkSucceeded(verificationResult.ProviderReference, verificationResult.StatusChangeReason, now);
                    await eventPublisher.PublishAsync(
                        new SuccessfulPaymentConfirmed
                        {
                            PaymentTransactionId = transaction.Id,
                            MerchantReference = transaction.MerchantReference,
                            PaymentIntent = transaction.PaymentIntent,
                            PaymentProviderId = transaction.PaymentProviderId,
                            Amount = transaction.Amount,
                            CurrencyId = transaction.CurrencyId,
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
                case PaymentProviderVerificationStatus.Failed when transaction.PaymentStatus != Contracts.Payments.PaymentStatus.Failed:
                    transaction.MarkFailed(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason,
                        now);
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
                case PaymentProviderVerificationStatus.Expired when transaction.PaymentStatus != Contracts.Payments.PaymentStatus.Expired:
                    transaction.MarkExpired(
                        verificationResult.ProviderReference,
                        verificationResult.FailureReason,
                        verificationResult.StatusChangeReason);
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
