namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed record ResolveLocationRequest(
    string AddressLine,
    string City,
    string State,
    string CountryCode);
