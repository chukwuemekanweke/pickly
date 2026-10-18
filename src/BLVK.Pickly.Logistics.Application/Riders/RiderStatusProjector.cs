using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders;

public sealed class RiderStatusProjector(IRiderStatusReader reader)
{
    public async Task<RiderStatusSnapshot> ProjectAsync(
        RiderProfile profile,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var facts = await reader.GetFactsAsync(profile, utcNow, cancellationToken);
        var hasFreshLocation = facts.Location is not null &&
            facts.Location.CapturedAtUtc >= utcNow.ToUniversalTime() - options.LocationFreshness;
        var operationalStatus = !profile.IsApproved
            ? profile.VerificationStatus.ToString()
            : facts.HasActiveDelivery
                ? "Busy"
                : !profile.HasEnabledAvailability
                    ? profile.AvailabilityStatus.ToString()
                    : hasFreshLocation
                        ? "Available"
                        : "StaleLocation";

        return new(
            profile.Id,
            profile.VehicleCategoryId,
            profile.VerificationStatus.ToString(),
            operationalStatus,
            profile.HasEnabledAvailability,
            hasFreshLocation,
            facts.Location?.CapturedAtUtc,
            facts.HasActiveDelivery,
            facts.PendingOfferCount);
    }
}
