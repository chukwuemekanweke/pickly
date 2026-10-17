using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DeliveryBookingByQuoteSpecification : Specification<DeliveryBookingConfirmation>
{
    public DeliveryBookingByQuoteSpecification(Guid senderId, Guid quoteId) =>
        Where(confirmation => confirmation.SenderId == senderId && confirmation.DeliveryQuoteId == quoteId);
}
