using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;

public sealed record LoginActivityHistoryReadModel(
    Guid LoginActivityId,
    LoginActivityType ActivityType,
    DateTimeOffset OccurredAtUtc,
    string IpAddress,
    string? DeviceName,
    string? DevicePlatform,
    string? BrowserName,
    string? City,
    string? State,
    string? Country);
