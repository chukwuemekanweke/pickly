namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public interface ITwoFactorOtpService
{
    Task<TwoFactorOtp> GenerateOtpAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken,
        int characterLength = 8,
        bool isAlphaNumeric = true,
        DateTimeOffset? expiresAtUtc = null);

    Task<bool> ValidateOtpAsync(
        Guid userId,
        string otp,
        OtpIntent intent,
        CancellationToken cancellationToken);

    Task<bool> OtpExistsAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken);

    Task<TwoFactorOtp?> GetActiveOtpAsync(
        Guid userId,
        OtpIntent intent,
        CancellationToken cancellationToken);
}
