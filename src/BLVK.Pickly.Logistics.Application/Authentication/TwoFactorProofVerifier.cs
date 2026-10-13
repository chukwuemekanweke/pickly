using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed class TwoFactorProofVerifier(IAuthenticationIdentityService identityService)
{
    public async Task<bool> VerifyAsync(
        AppUser user,
        TwoFactorVerificationMethod method,
        string code)
    {
        return method switch
        {
            TwoFactorVerificationMethod.Authenticator =>
                await identityService.VerifyAuthenticatorTokenAsync(user, code),
            TwoFactorVerificationMethod.RecoveryCode =>
                (await identityService.RedeemTwoFactorRecoveryCodeAsync(user, code)).Succeeded,
            _ => false
        };
    }
}
