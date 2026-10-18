namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public sealed record RiderStatusSnapshot(
    Guid RiderProfileId,
    Guid VehicleCategoryId,
    string VerificationStatus,
    string OperationalStatus,
    bool AvailabilityEnabled,
    bool HasFreshLocation,
    DateTimeOffset? LocationCapturedAtUtc,
    bool HasActiveDelivery,
    int PendingOfferCount);
