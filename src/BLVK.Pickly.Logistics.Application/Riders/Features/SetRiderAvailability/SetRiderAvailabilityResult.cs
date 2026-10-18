using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;

public sealed record SetRiderAvailabilityResult(
    SetRiderAvailabilityStatus Status,
    RiderStatusSnapshot? RiderStatus = null);
