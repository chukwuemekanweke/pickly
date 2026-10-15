using BLVK.Pickly.Logistics.Application.Deliveries;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record ListPickupLocationsResponse(
    IReadOnlyList<PickupLocationResponse> Items,
    string? NextCursor,
    bool HasMore);
