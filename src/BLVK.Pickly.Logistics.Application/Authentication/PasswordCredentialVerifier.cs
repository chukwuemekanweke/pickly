using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed class PasswordCredentialVerifier(IAuthenticationIdentityService identityService)
{
    public async Task<PasswordCredentialVerificationStatus> VerifyAsync(AppUser user, string password)
    {
        if (await identityService.IsLockedOutAsync(user))
        {
            return PasswordCredentialVerificationStatus.Locked;
        }

        if (!await identityService.CheckPasswordAsync(user, password))
        {
            var failure = await identityService.AccessFailedAsync(user);
            if (!failure.Succeeded)
            {
                throw new InvalidOperationException("Failed to record the authentication attempt.");
            }

            return await identityService.IsLockedOutAsync(user)
                ? PasswordCredentialVerificationStatus.Locked
                : PasswordCredentialVerificationStatus.Invalid;
        }

        var reset = await identityService.ResetAccessFailedCountAsync(user);
        if (!reset.Succeeded)
        {
            throw new InvalidOperationException("Failed to reset the authentication failure count.");
        }

        return PasswordCredentialVerificationStatus.Success;
    }
}
