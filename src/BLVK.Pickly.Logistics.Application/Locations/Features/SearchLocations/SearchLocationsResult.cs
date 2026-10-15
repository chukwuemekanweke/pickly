namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

public sealed record SearchLocationsResult(
    SearchLocationsStatus Status,
    IReadOnlyList<LocationSummaryResponse> Items);
