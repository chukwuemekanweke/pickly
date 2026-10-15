namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record ListPickupLocationsRequest(int Limit = 20, string? Cursor = null);
