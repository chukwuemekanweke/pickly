using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryPaymentAttemptByTransactionSpecification : Specification<DeliveryPaymentAttempt>
{
    public DeliveryPaymentAttemptByTransactionSpecification(Guid paymentTransactionId, bool enableTracking = false)
    {
        Where(attempt => attempt.PaymentTransactionId == paymentTransactionId);
        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
