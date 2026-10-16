using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed record GetNearbyLocationsRequest(
    Guid LocationId,
    int RadiusMeters = 1000,
    int Limit = 10,
    LocationType? LocationType = null);
