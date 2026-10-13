using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;

public sealed record RefreshSessionResult(
    RefreshSessionStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null);

public enum RefreshSessionStatus
{
    Success = 1,
    InvalidRefreshToken = 2,
    EmailNotVerified = 3,
    AccountLocked = 4
}
