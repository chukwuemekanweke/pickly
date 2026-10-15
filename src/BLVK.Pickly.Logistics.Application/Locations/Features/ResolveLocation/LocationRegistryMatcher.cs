using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Specifications;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed class LocationRegistryMatcher(
    IRepository<Location> locations,
    ResolveLocationTelemetry telemetry)
{
    public async Task<LocationRegistryMatch?> FindAsync(
        string resolutionKey,
        string normalizedAddress,
        CancellationToken cancellationToken)
    {
        var byResolutionKey = await locations.FirstOrDefaultAsync(
            new LocationByResolutionKeySpecification(resolutionKey), cancellationToken);
        if (byResolutionKey is { IsUsable: true })
        {
            return new LocationRegistryMatch(byResolutionKey, "resolution_key");
        }

        var byAddress = await locations.ListAsync(
            new UsableLocationsByNormalizedAddressSpecification(normalizedAddress), cancellationToken);
        return byAddress.Count == 1
            ? new LocationRegistryMatch(byAddress[0], "normalized_address")
            : null;
    }

    public ResolveLocationResult ToResult(ResolveLocationCommand request, LocationRegistryMatch match)
    {
        telemetry.Record(
            request,
            Observability.EventNames.Locations.RegistryHit,
            new Dictionary<string, string>
            {
                [Observability.PropertyNames.Locations.LocationId] = match.Location.Id.ToString(),
                [Observability.PropertyNames.Locations.MatchKind] = match.MatchKind
            });

        return new ResolveLocationResult(
            ResolveLocationStatus.Resolved,
            LocationResponse.FromEntity(match.Location),
            FromRegistry: true);
    }
}
