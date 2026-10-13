namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record TwoFactorOtp(string Code, DateTimeOffset ExpiresAtUtc);
