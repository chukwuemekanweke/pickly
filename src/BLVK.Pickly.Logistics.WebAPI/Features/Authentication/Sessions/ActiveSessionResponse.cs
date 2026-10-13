namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

public sealed record ActiveSessionResponse(Guid SessionId, string? DeviceName, string? DevicePlatform,
    string? BrowserName, string UserAgent, string FirstIpAddress, string LastIpAddress,
    DateTimeOffset CreatedAtUtc, DateTimeOffset LastActiveAtUtc, DateTimeOffset ExpiresAtUtc,
    bool IsCurrent, string? City, string? State, string? Country);
