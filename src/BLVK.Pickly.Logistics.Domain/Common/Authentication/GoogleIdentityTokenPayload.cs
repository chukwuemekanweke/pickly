namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record GoogleIdentityTokenPayload(
    string Subject,
    string Email,
    string? DisplayName,
    bool EmailVerified = true,
    string? HostedDomain = null);
