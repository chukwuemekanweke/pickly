namespace BLVK.Pickly.Logistics.Domain.Authentication.Services;

public sealed record IpGeolocation(
    string? City,
    string? State,
    string? Country);
