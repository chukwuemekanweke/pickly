using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.Operations.ReadModels;

public sealed record RiderAvailabilityCount(
    RiderAvailabilityStatus AvailabilityStatus,
    bool HasActiveDelivery,
    int Count);
