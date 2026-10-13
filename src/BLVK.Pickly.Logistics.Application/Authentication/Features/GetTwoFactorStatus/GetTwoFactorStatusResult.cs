namespace BLVK.Pickly.Logistics.Application.Authentication.Features.GetTwoFactorStatus;

public sealed record GetTwoFactorStatusResult(
    GetTwoFactorStatusStatus Status,
    bool Enabled = false,
    int RecoveryCodesRemaining = 0);

public enum GetTwoFactorStatusStatus
{
    Success = 1,
    NotAuthenticated = 2
}
