namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

public sealed record PickupLocationAddressRequest(
    string AddressLine,
    string? AdditionalInformation,
    string City,
    string State,
    string CountryCode,
    decimal Latitude,
    decimal Longitude);
