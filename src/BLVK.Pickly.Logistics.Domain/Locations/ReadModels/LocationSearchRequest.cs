namespace BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

public sealed record LocationSearchRequest(
    IReadOnlyList<string> Terms,
    string? CountryCode,
    int Limit);
