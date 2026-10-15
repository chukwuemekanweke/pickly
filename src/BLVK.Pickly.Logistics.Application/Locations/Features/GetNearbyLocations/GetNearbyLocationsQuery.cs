using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;

public sealed record GetNearbyLocationsQuery(
    Guid LocationId,
    int RadiusMeters,
    int Limit,
    LocationType? LocationType,
    ActorContext ActorContext);
