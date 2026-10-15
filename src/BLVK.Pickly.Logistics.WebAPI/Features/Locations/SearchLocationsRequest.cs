namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public sealed record SearchLocationsRequest(string? Q, string? CountryCode = null, int Limit = 10);
