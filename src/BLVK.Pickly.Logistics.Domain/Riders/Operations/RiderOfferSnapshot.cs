namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public sealed record RiderOfferSnapshot(
    Guid OfferId,
    Guid DeliveryId,
    string TrackingId,
    string PickupAddress,
    string DropOffArea,
    Guid VehicleCategoryId,
    int ParcelCount,
    decimal ApproximateWeightKg,
    DateTimeOffset ExpiresAtUtc);
