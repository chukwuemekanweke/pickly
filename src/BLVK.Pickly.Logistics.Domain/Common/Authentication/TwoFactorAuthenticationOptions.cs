namespace BLVK.Pickly.Logistics.Domain.Common.Authentication;

public sealed class TwoFactorAuthenticationOptions
{
    public const string SectionName = "Authentication:TwoFactor";

    public string Issuer { get; init; } = "BLVK.Pickly.Logistics";
    public TimeSpan ChallengeLifetime { get; init; } = TimeSpan.FromMinutes(5);
    public int ChallengeAttempts { get; init; } = 5;
    public int RecoveryCodeCount { get; init; } = 10;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new InvalidOperationException("Two-factor issuer is required.");
        if (ChallengeLifetime < TimeSpan.FromMinutes(1) || ChallengeLifetime > TimeSpan.FromMinutes(10))
            throw new InvalidOperationException("Two-factor challenge lifetime must be between one and ten minutes.");
        if (ChallengeAttempts is < 1 or > 10)
            throw new InvalidOperationException("Two-factor challenge attempts must be between one and ten.");
        if (RecoveryCodeCount is < 1 or > 20)
            throw new InvalidOperationException("Two-factor recovery code count must be between one and twenty.");
    }
}
