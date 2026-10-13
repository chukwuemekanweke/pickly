using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record TwoFactorChallenge(
    string Token,
    Guid AppUserId,
    Guid StakeholderId,
    Guid TenantId,
    AuthenticationMethod AuthenticationMethod,
    ActorContext ActorContext,
    string IpAddress,
    string UserAgent,
    DateTimeOffset ExpiresAtUtc,
    int RemainingAttempts);
