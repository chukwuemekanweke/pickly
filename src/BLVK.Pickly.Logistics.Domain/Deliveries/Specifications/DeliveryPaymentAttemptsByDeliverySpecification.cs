using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryPaymentAttemptsByDeliverySpecification : Specification<DeliveryPaymentAttempt>
{
    public DeliveryPaymentAttemptsByDeliverySpecification(Guid deliveryId, bool enableTracking = false)
    {
        Where(attempt => attempt.DeliveryId == deliveryId);
        ApplyOrderByDescending(attempt => attempt.CreatedAtUtc);
        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
