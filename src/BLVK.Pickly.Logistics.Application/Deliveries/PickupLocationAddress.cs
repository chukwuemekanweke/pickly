using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Deliveries;

public sealed record PickupLocationAddress(
    string AddressLine,
    string? AdditionalInformation,
    string City,
    string State,
    string CountryCode,
    decimal Latitude,
    decimal Longitude)
{
    public DeliveryAddress ToDomain() => DeliveryAddress.Create(
        AddressLine,
        AdditionalInformation,
        City,
        State,
        CountryCode,
        Latitude,
        Longitude);
}
