using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class TrackedDeliveryQuoteByOwnerAndIdSpecification : Specification<DeliveryQuote>
{
    public TrackedDeliveryQuoteByOwnerAndIdSpecification(Guid senderId, Guid quoteId)
    {
        Where(quote => quote.Id == quoteId && quote.SenderId == senderId);
        AddInclude(quote => quote.Items);
        EnableTracking();
    }
}
