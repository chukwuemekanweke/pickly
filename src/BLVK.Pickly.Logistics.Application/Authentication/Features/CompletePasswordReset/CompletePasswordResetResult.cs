namespace BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;

public sealed record CompletePasswordResetResult(
    CompletePasswordResetStatus Status,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null);

public enum CompletePasswordResetStatus
{
    Success = 1,
    InvalidOtp = 2,
    UserNotFound = 3,
    ValidationFailed = 4
}
