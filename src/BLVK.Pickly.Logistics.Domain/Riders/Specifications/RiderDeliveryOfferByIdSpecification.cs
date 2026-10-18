using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Specifications;

public sealed class RiderDeliveryOfferByIdSpecification : Specification<RiderDeliveryOffer>
{
    public RiderDeliveryOfferByIdSpecification(Guid offerId) => Where(offer => offer.Id == offerId);
}
