namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;

public sealed record ListPickupLocationsResult(
    ListPickupLocationsStatus Status,
    IReadOnlyList<PickupLocationResponse> Items,
    string? NextCursor,
    bool HasMore);

public enum ListPickupLocationsStatus
{
    Success = 1,
    NotAuthenticated = 2
}
