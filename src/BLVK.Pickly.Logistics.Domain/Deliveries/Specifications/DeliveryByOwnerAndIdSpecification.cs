using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryByOwnerAndIdSpecification : Specification<Delivery>
{
    public DeliveryByOwnerAndIdSpecification(Guid senderId, Guid deliveryId, bool enableTracking = false)
    {
        Where(delivery => delivery.Id == deliveryId && delivery.SenderId == senderId);
        AddInclude(delivery => delivery.Items);
        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
