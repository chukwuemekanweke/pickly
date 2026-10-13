using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;

public sealed record SignInResult(
    SignInStatus Status,
    AuthenticationTokens? Tokens,
    DateTimeOffset? LockedUntilUtc = null,
    TwoFactorChallenge? Challenge = null);

public enum SignInStatus
{
    Success = 1,
    InvalidCredentials = 2,
    EmailNotVerified = 3,
    AccountLocked = 4,
    RequiresTwoFactor = 5
}
