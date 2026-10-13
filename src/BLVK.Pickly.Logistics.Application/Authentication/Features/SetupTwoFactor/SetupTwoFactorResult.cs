namespace BLVK.Pickly.Logistics.Application.Authentication.Features.SetupTwoFactor;

public sealed record SetupTwoFactorResult(
    SetupTwoFactorStatus Status,
    string? SharedKey = null,
    string? AuthenticatorUri = null);

public enum SetupTwoFactorStatus
{
    Success = 1,
    NotAuthenticated = 2,
    AlreadyEnabled = 3,
    Failed = 4
}
