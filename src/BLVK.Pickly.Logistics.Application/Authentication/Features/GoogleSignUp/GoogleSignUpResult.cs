namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;

using BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed record GoogleSignUpResult(
    GoogleSignUpStatus Status,
    string? Email = null,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    AuthenticationTokens? Tokens = null,
    DateTimeOffset? RetryAtUtc = null);

public enum GoogleSignUpStatus
{
    Success = 1,
    EmailVerificationRequired = 2,
    DuplicateEmail = 3,
    DuplicateGoogleAccount = 4,
    ValidationFailed = 5,
    CountryMismatch = 6,
    GoogleFlowInvalid = 7,
    GoogleFlowExpired = 8,
    GoogleFlowConsumed = 9
}
