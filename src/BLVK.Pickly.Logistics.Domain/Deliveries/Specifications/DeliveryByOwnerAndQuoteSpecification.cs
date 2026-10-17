using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryByOwnerAndQuoteSpecification : Specification<Delivery>
{
    public DeliveryByOwnerAndQuoteSpecification(Guid senderId, Guid quoteId)
    {
        Where(delivery => delivery.SenderId == senderId && delivery.DeliveryQuoteId == quoteId);
        AddInclude(delivery => delivery.Items);
    }
}
