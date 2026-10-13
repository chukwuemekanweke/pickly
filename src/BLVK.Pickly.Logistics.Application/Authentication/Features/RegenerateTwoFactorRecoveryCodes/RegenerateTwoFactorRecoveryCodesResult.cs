namespace BLVK.Pickly.Logistics.Application.Authentication.Features.RegenerateTwoFactorRecoveryCodes;

public sealed record RegenerateTwoFactorRecoveryCodesResult(
    RegenerateTwoFactorRecoveryCodesStatus Status,
    IReadOnlyList<string>? RecoveryCodes = null);

public enum RegenerateTwoFactorRecoveryCodesStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotEnabled = 3,
    InvalidProof = 4,
    Failed = 5
}
