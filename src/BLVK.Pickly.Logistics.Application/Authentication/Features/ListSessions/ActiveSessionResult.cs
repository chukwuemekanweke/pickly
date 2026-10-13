namespace BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;

public sealed record ActiveSessionResult(Guid SessionId, string? DeviceName, string? DevicePlatform,
    string? BrowserName, string UserAgent, string FirstIpAddress, string LastIpAddress,
    DateTimeOffset CreatedAtUtc, DateTimeOffset LastActiveAtUtc, DateTimeOffset ExpiresAtUtc,
    bool IsCurrent, string? City, string? State, string? Country);
