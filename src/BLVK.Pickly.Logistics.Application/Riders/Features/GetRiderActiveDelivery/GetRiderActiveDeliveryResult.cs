using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderActiveDelivery;

public sealed record GetRiderActiveDeliveryResult(
    GetRiderActiveDeliveryStatus Status,
    RiderAssignedDeliverySnapshot? Delivery = null);
