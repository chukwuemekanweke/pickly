using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using System.Security.Cryptography;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class TwoFactorOtpService(IJsonCache cache, TimeProvider timeProvider) : ITwoFactorOtpService
{
    private const string NumericCharacters = "0123456789";
    private const string AlphaNumericCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public async Task<TwoFactorOtp> GenerateOtpAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken,
        int characterLength = 8,
        bool isAlphaNumeric = true,
        DateTimeOffset? expiresAtUtc = null)
    {
        if (characterLength <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(characterLength), "OTP character length must be greater than zero.");
        }

        var now = timeProvider.GetUtcNow();
        var lifetime = ResolveLifetime(intent);
        var resolvedExpiresAtUtc = expiresAtUtc ?? now.Add(lifetime);
        var remainingLifetime = resolvedExpiresAtUtc - now;
        if (remainingLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "OTP expiry must be in the future.");
        }

        var generatedOtp = new TwoFactorOtp(
            GenerateCode(characterLength, isAlphaNumeric),
            resolvedExpiresAtUtc);

        await cache.SetAsync(
            BuildCacheKey(userId, intent),
            new CachedTwoFactorOtp(
                generatedOtp.Code,
                generatedOtp.ExpiresAtUtc,
                0),
            remainingLifetime,
            cancellationToken);

        return generatedOtp;
    }

    public async Task<bool> ValidateOtpAsync(
        Guid userId,
        string otp,
        OtpIntent intent,
        CancellationToken cancellationToken)
    {
        var cachedOtp = await GetActiveAsync(userId, intent, cancellationToken);
        if (cachedOtp is null)
        {
            return false;
        }

        var normalizedOtp = otp.Trim().ToUpperInvariant();
        if (string.Equals(cachedOtp.Code, normalizedOtp, StringComparison.Ordinal))
        {
            await cache.RemoveAsync(BuildCacheKey(userId, intent), cancellationToken);
            return true;
        }

        var failedAttempts = cachedOtp.FailedAttempts + 1;
        if (failedAttempts >= ResolveMaxFailedAttempts(intent))
        {
            await cache.RemoveAsync(BuildCacheKey(userId, intent), cancellationToken);
            return false;
        }

        var remainingLifetime = cachedOtp.ExpiresAtUtc - timeProvider.GetUtcNow();
        if (remainingLifetime <= TimeSpan.Zero)
        {
            await cache.RemoveAsync(BuildCacheKey(userId, intent), cancellationToken);
            return false;
        }

        await cache.SetAsync(
            BuildCacheKey(userId, intent),
            cachedOtp with { FailedAttempts = failedAttempts },
            remainingLifetime,
            cancellationToken);

        return false;
    }

    public async Task<bool> OtpExistsAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken) =>
        await GetActiveAsync(userId, intent, cancellationToken) is not null;

    public async Task<TwoFactorOtp?> GetActiveOtpAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken)
    {
        var cachedOtp = await GetActiveAsync(userId, intent, cancellationToken);
        return cachedOtp is null ? null : new TwoFactorOtp(cachedOtp.Code, cachedOtp.ExpiresAtUtc);
    }

    private async Task<CachedTwoFactorOtp?> GetActiveAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken)
    {
        var cachedOtp = await cache.GetAsync<CachedTwoFactorOtp>(BuildCacheKey(userId, intent), cancellationToken);
        if (cachedOtp is null)
        {
            return null;
        }

        if (cachedOtp.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            await cache.RemoveAsync(BuildCacheKey(userId, intent), cancellationToken);
            return null;
        }

        return cachedOtp;
    }

    private static string BuildCacheKey(Guid userId, OtpIntent intent) =>
        $"authentication:otp:{intent}:{userId:N}";

    private static string GenerateCode(int characterLength, bool isAlphaNumeric)
    {
        var characterSet = isAlphaNumeric ? AlphaNumericCharacters : NumericCharacters;
        var code = new char[characterLength];

        for (var index = 0; index < code.Length; index++)
        {
            code[index] = characterSet[RandomNumberGenerator.GetInt32(characterSet.Length)];
        }

        return new string(code);
    }

    private static TimeSpan ResolveLifetime(OtpIntent intent) =>
        intent switch
        {
            OtpIntent.PasswordReset => TimeSpan.FromMinutes(2),
            OtpIntent.EmailConfirmation => AuthenticationOtpDefaults.EmailConfirmationLifetime,
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unsupported OTP intent.")
        };

    private static int ResolveMaxFailedAttempts(OtpIntent intent) =>
        intent switch
        {
            OtpIntent.PasswordReset => 5,
            OtpIntent.EmailConfirmation => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(intent), intent, "Unsupported OTP intent.")
        };

    private sealed record CachedTwoFactorOtp(
        string Code,
        DateTimeOffset ExpiresAtUtc,
        int FailedAttempts);
}
