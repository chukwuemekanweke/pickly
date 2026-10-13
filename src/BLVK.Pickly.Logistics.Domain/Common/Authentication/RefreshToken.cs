namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record RefreshToken(string Value, DateTimeOffset ExpiresAtUtc);
