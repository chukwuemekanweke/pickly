namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;

public sealed record GetLoginActivityHistoryResponse(
    Guid Id,
    string ActivityType,
    DateTimeOffset OccurredAtUtc,
    string IpAddress,
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName,
    string? City,
    string? State,
    string? Country);
