using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

public sealed record NearbyLocationsRequest(
    GeographicCoordinates Anchor,
    int RadiusMeters,
    int Limit,
    Guid? ExcludeLocationId,
    LocationType? LocationType);
