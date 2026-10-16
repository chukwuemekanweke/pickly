namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record CreatePickupLocationRequest(
    Guid LocationId,
    string Label,
    string? AdditionalDirections = null,
    bool IsDefault = false);
