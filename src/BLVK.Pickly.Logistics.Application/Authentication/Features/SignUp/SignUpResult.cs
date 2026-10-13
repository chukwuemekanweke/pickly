namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;

public sealed record SignUpResult(
    SignUpStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null,
    DateTimeOffset? RetryAtUtc = null);

public enum SignUpStatus
{
    Accepted = 1,
    DuplicateEmail = 2,
    ValidationFailed = 3,
    CountryMismatch = 4
}
