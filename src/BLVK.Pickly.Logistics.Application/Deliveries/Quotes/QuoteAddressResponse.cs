using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed record QuoteAddressResponse(string AddressLine, string? AdditionalInformation, string City, string State, string CountryCode, decimal Latitude, decimal Longitude)
{
    public static QuoteAddressResponse From(DeliveryAddress address) => new(address.AddressLine, address.AdditionalInformation, address.City, address.State, address.CountryCode, address.Latitude, address.Longitude);
}
