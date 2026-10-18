using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;

public sealed record AcceptRiderOfferResult(
    AcceptRiderOfferStatus Status,
    RiderAssignedDeliverySnapshot? Delivery = null);
