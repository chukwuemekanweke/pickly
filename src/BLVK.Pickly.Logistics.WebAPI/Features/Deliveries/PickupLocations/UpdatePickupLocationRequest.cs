namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record UpdatePickupLocationRequest(
    string Label,
    PickupLocationAddressRequest Address);
