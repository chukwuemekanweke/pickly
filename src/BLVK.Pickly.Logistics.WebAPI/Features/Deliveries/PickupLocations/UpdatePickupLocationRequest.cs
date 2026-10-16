namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record UpdatePickupLocationRequest(
    Guid LocationId,
    string Label,
    string? AdditionalDirections = null);
