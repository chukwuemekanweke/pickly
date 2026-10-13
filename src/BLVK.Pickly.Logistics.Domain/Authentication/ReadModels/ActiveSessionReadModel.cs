namespace BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

public sealed record ActiveSessionReadModel(
    Guid SessionId,
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName,
    string UserAgent,
    string FirstIpAddress,
    string LastIpAddress,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastActiveAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? City,
    string? State,
    string? Country);
