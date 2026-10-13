namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record GoogleAuthenticationFlow(
    string Token,
    string Nonce,
    Guid TenantId,
    GoogleAuthenticationFlowState State,
    DateTimeOffset ExpiresAtUtc,
    string? Subject = null,
    string? Email = null,
    bool EmailVerified = false,
    string? HostedDomain = null);
