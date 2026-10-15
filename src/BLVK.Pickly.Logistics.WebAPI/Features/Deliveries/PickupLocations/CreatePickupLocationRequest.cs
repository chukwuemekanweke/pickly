namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record CreatePickupLocationRequest(
    string Label,
    PickupLocationAddressRequest Address,
    bool IsDefault = false);
