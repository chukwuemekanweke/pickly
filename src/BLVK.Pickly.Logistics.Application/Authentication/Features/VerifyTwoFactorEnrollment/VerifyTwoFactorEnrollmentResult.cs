namespace BLVK.Pickly.Logistics.Application.Authentication.Features.VerifyTwoFactorEnrollment;

public sealed record VerifyTwoFactorEnrollmentResult(
    VerifyTwoFactorEnrollmentStatus Status,
    IReadOnlyList<string>? RecoveryCodes = null);

public enum VerifyTwoFactorEnrollmentStatus
{
    Success = 1,
    NotAuthenticated = 2,
    AlreadyEnabled = 3,
    InvalidCode = 4,
    Failed = 5
}
