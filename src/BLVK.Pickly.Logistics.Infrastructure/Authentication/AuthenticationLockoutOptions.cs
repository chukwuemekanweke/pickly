namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class AuthenticationLockoutOptions
{
    public const string SectionName = "Authentication:Lockout";

    public int MaxFailedAttempts { get; init; } = 5;
    public TimeSpan Duration { get; init; } = TimeSpan.FromHours(12);

    public void Validate()
    {
        if (MaxFailedAttempts <= 0)
        {
            throw new InvalidOperationException($"{nameof(MaxFailedAttempts)} must be greater than zero.");
        }

        if (Duration <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(Duration)} must be greater than zero.");
        }
    }
}
