using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryQuoteByOwnerAndIdSpecification : Specification<DeliveryQuote>
{
    public DeliveryQuoteByOwnerAndIdSpecification(Guid senderId, Guid quoteId)
    {
        Where(x => x.Id == quoteId && x.SenderId == senderId);
        AddInclude(x => x.Items);
    }
}
