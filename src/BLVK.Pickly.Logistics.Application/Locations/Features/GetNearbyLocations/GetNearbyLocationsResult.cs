namespace BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;

public sealed record GetNearbyLocationsResult(
    GetNearbyLocationsStatus Status,
    IReadOnlyList<NearbyLocationResponse> Items);
