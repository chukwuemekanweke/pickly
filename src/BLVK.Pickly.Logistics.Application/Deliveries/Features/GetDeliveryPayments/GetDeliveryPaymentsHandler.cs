using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryPayments;

public sealed class GetDeliveryPaymentsHandler(
    IRepository<Delivery> deliveries,
    IRepository<DeliveryPaymentAttempt> deliveryPaymentAttempts,
    IRepository<PaymentTransaction> paymentTransactions,
    IRepository<PaymentProvider> paymentProviders,
    IRepository<Currency> currencies)
{
    public async Task<GetDeliveryPaymentsResult> HandleAsync(
        GetDeliveryPaymentsQuery query,
        CancellationToken cancellationToken)
    {
        var senderId = query.ActorContext.StakeholderId;
        if (!senderId.HasValue || senderId == Guid.Empty)
        {
            return new(GetDeliveryPaymentsStatus.NotAuthenticated);
        }

        var delivery = await deliveries.FirstOrDefaultAsync(
            new DeliveryByOwnerAndIdSpecification(senderId.Value, query.DeliveryId), cancellationToken);
        if (delivery is null)
        {
            return new(GetDeliveryPaymentsStatus.NotFound);
        }

        var associations = await deliveryPaymentAttempts.ListAsync(
            new DeliveryPaymentAttemptsByDeliverySpecification(delivery.Id), cancellationToken);
        var attempts = new List<PaymentTransaction>(associations.Count);
        foreach (var association in associations)
        {
            var payment = await paymentTransactions.GetByIdAsync(association.PaymentTransactionId, cancellationToken);
            if (payment is not null)
            {
                attempts.Add(payment);
            }
        }
        var providerNames = new Dictionary<Guid, string>();
        foreach (var providerId in attempts.Select(attempt => attempt.PaymentProviderId).Distinct())
        {
            providerNames[providerId] = (await paymentProviders.GetByIdAsync(providerId, cancellationToken))?.ProviderName ?? string.Empty;
        }

        var currency = await currencies.GetByIdAsync(delivery.Charge.CurrencyId, cancellationToken);
        return new(
            GetDeliveryPaymentsStatus.Success,
            new DeliveryPaymentsResponse(
                delivery.Id,
                delivery.PaymentStatus.ToString(),
                delivery.OrderType == DeliveryOrderType.PayBeforeDelivery && delivery.PaymentStatus != DeliveryPaymentStatus.Paid,
                delivery.Charge.Amount,
                delivery.Charge.CurrencyId,
                currency?.CurrencyCode ?? string.Empty,
                attempts.Select(attempt => new DeliveryPaymentAttemptResponse(
                    attempt.Id,
                    attempt.PaymentProviderId,
                    providerNames[attempt.PaymentProviderId],
                    attempt.MerchantReference,
                    attempt.PaymentStatus.ToString(),
                    attempt.CreatedAtUtc,
                    attempt.ExpiresAtUtc,
                    attempt.PaymentStatus is Contracts.Payments.PaymentStatus.PendingInitiation or Contracts.Payments.PaymentStatus.Initiated
                        ? attempt.ProviderPayloadMetadata
                        : new Dictionary<string, string>())).ToArray()));
    }
}
