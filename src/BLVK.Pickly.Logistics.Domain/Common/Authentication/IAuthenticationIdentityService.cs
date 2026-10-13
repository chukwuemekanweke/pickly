using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface IAuthenticationIdentityService
{
    Task<AppUser?> FindByIdAsync(Guid userId);
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByLoginAsync(string loginProvider, string providerKey);
    Task<string> GetSecurityStampAsync(AppUser user);
    Task<bool> GetTwoFactorEnabledAsync(AppUser user);
    Task<string?> GetAuthenticatorKeyAsync(AppUser user);
    Task<IdentityResult> ResetAuthenticatorKeyAsync(AppUser user);
    Task<bool> VerifyAuthenticatorTokenAsync(AppUser user, string token);
    Task<IdentityResult> SetTwoFactorEnabledAsync(AppUser user, bool enabled);
    Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(AppUser user, int number);
    Task<int> CountRecoveryCodesAsync(AppUser user);
    Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(AppUser user, string code);
    Task<IdentityResult> UpdateSecurityStampAsync(AppUser user);
    Task<bool> IsLockedOutAsync(AppUser user);
    Task<DateTimeOffset?> GetLockoutEndUtcAsync(AppUser user);
    Task<IdentityResult> CreateAsync(AppUser user);
    Task<IdentityResult> CreateAsync(AppUser user, string password);
    Task<IdentityResult> AddLoginAsync(AppUser user, string loginProvider, string providerKey, string displayName);
    Task<IdentityResult> ResetPasswordAsync(AppUser user, string newPassword);
    Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword);
    Task<bool> CheckPasswordAsync(AppUser user, string password);
    Task<IdentityResult> AccessFailedAsync(AppUser user);
    Task<IdentityResult> ResetAccessFailedCountAsync(AppUser user);
    Task<IdentityResult> UpdateAsync(AppUser user);
}
