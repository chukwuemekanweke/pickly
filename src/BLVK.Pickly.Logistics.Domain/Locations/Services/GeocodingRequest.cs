namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

public sealed record GeocodingRequest(
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    int MaxCandidates);
