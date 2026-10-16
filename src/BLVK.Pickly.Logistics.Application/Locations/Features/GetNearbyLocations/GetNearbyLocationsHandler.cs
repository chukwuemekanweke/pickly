using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;

/// <summary>
/// Lists actual registered public locations around an existing resolved location. The anchor itself is excluded and
/// no alternative coordinates are ever synthesized.
/// </summary>
public sealed class GetNearbyLocationsHandler(
    ILocationReadModelRepository repository,
    IOptions<LocationResolutionOptions> options)
{
    public async Task<GetNearbyLocationsResult> HandleAsync(
        GetNearbyLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var stakeholderId = query.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
        {
            return new GetNearbyLocationsResult(GetNearbyLocationsStatus.NotAuthenticated, []);
        }

        var anchor = await repository.GetByIdAsync(query.LocationId, cancellationToken);
        if (anchor is null || anchor.ResolutionStatus != LocationResolutionStatus.Resolved)
        {
            return new GetNearbyLocationsResult(GetNearbyLocationsStatus.AnchorNotFound, []);
        }

        var settings = options.Value;
        var nearby = await repository.FindNearbyAsync(
            new NearbyLocationsRequest(
                GeographicCoordinates.Create(anchor.Latitude, anchor.Longitude),
                Math.Clamp(query.RadiusMeters, 1, settings.MaxNearbyRadiusMeters),
                Math.Clamp(query.Limit, 1, settings.MaxResults),
                anchor.Id,
                query.LocationType),
            cancellationToken);

        return new GetNearbyLocationsResult(
            GetNearbyLocationsStatus.Success,
            nearby.Select(NearbyLocationResponse.FromReadModel).ToArray());
    }
}
