using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class IdentityUserService(UserManager<AppUser> userManager) : IAuthenticationIdentityService
{
    public Task<AppUser?> FindByIdAsync(Guid userId) =>
        userManager.FindByIdAsync(userId.ToString());

    public Task<AppUser?> FindByEmailAsync(string email) =>
        userManager.FindByEmailAsync(email);

    public Task<AppUser?> FindByLoginAsync(string loginProvider, string providerKey) =>
        userManager.FindByLoginAsync(loginProvider, providerKey);

    public Task<string> GetSecurityStampAsync(AppUser user) =>
        userManager.GetSecurityStampAsync(user);

    public Task<bool> GetTwoFactorEnabledAsync(AppUser user) =>
        userManager.GetTwoFactorEnabledAsync(user);

    public Task<string?> GetAuthenticatorKeyAsync(AppUser user) =>
        userManager.GetAuthenticatorKeyAsync(user);

    public Task<IdentityResult> ResetAuthenticatorKeyAsync(AppUser user) =>
        userManager.ResetAuthenticatorKeyAsync(user);

    public Task<bool> VerifyAuthenticatorTokenAsync(AppUser user, string token) =>
        userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, token);

    public Task<IdentityResult> SetTwoFactorEnabledAsync(AppUser user, bool enabled) =>
        userManager.SetTwoFactorEnabledAsync(user, enabled);

    public Task<IEnumerable<string>?> GenerateNewTwoFactorRecoveryCodesAsync(AppUser user, int number) =>
        userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, number);

    public Task<int> CountRecoveryCodesAsync(AppUser user) =>
        userManager.CountRecoveryCodesAsync(user);

    public Task<IdentityResult> RedeemTwoFactorRecoveryCodeAsync(AppUser user, string code) =>
        userManager.RedeemTwoFactorRecoveryCodeAsync(user, NormalizeRecoveryCode(code));

    public Task<IdentityResult> UpdateSecurityStampAsync(AppUser user) =>
        userManager.UpdateSecurityStampAsync(user);

    public Task<bool> IsLockedOutAsync(AppUser user) =>
        userManager.IsLockedOutAsync(user);

    public async Task<DateTimeOffset?> GetLockoutEndUtcAsync(AppUser user)
    {
        var lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
        return lockoutEnd?.ToUniversalTime();
    }

    public Task<IdentityResult> CreateAsync(AppUser user) =>
        userManager.CreateAsync(user);

    public Task<IdentityResult> CreateAsync(AppUser user, string password) =>
        userManager.CreateAsync(user, password);

    public Task<IdentityResult> AddLoginAsync(AppUser user, string loginProvider, string providerKey, string displayName) =>
        userManager.AddLoginAsync(user, new UserLoginInfo(loginProvider, providerKey, displayName));

    public async Task<IdentityResult> ResetPasswordAsync(AppUser user, string newPassword)
    {
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        return await userManager.ResetPasswordAsync(user, resetToken, newPassword);
    }

    public Task<IdentityResult> ChangePasswordAsync(AppUser user, string currentPassword, string newPassword) =>
        userManager.ChangePasswordAsync(user, currentPassword, newPassword);

    public Task<bool> CheckPasswordAsync(AppUser user, string password) =>
        userManager.CheckPasswordAsync(user, password);

    public Task<IdentityResult> AccessFailedAsync(AppUser user) =>
        userManager.AccessFailedAsync(user);

    public Task<IdentityResult> ResetAccessFailedCountAsync(AppUser user) =>
        userManager.ResetAccessFailedCountAsync(user);

    public Task<IdentityResult> UpdateAsync(AppUser user) =>
        userManager.UpdateAsync(user);

    private static string NormalizeRecoveryCode(string code)
    {
        var compactCode = code
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return compactCode.Length == 10
            ? compactCode.Insert(5, "-")
            : code.Trim();
    }
}
