using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompleteTwoFactorChallenge;

public sealed record CompleteTwoFactorChallengeResult(
    CompleteTwoFactorChallengeStatus Status,
    AuthenticationTokens? Tokens = null,
    DateTimeOffset? LockedUntilUtc = null);

public enum CompleteTwoFactorChallengeStatus
{
    Success = 1,
    InvalidCode = 2,
    InvalidChallenge = 3,
    ExpiredChallenge = 4,
    ConsumedChallenge = 5,
    ExhaustedChallenge = 6,
    AccountLocked = 7,
    EmailNotVerified = 8
}
