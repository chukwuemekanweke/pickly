namespace BLVK.Pickly.Logistics.Application.Authentication.Features.DisableTwoFactor;

public enum DisableTwoFactorResult
{
    Success = 1,
    NotAuthenticated = 2,
    NotEnabled = 3,
    InvalidProof = 4,
    Failed = 5
}
