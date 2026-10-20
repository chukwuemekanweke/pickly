namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;

public static class RateLimitingPolicyNames
{
    public const string SignInPolicy = "sign-in-policy";
    public const string RefreshSessionPolicy = "refresh-session-policy";
    public const string SignUpPolicy = "sign-up-policy";
    public const string EmailOperationsPolicy = "email-operations-policy";
    public const string PasswordResetPolicy = "password-reset-policy";
    public const string TwoFactorVerificationPolicy = "two-factor-verification-policy";
    public const string LocationSearchPolicy = "location-search-policy";
    public const string LocationResolutionPolicy = "location-resolution-policy";
    public const string RiderLocationPolicy = "rider-location-policy";
}
