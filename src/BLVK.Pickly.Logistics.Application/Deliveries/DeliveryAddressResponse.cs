using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Deliveries;

public sealed record DeliveryAddressResponse(
    string AddressLine,
    string? AdditionalInformation,
    string City,
    string State,
    string CountryCode,
    decimal Latitude,
    decimal Longitude)
{
    public static DeliveryAddressResponse From(DeliveryAddress address) =>
        new(
            address.AddressLine,
            address.AdditionalInformation,
            address.City,
            address.State,
            address.CountryCode,
            address.Latitude,
            address.Longitude);
}
