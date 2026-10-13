namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class GoogleAuthenticationOptions
{
    public const string SectionName = "Authentication:Google";

    public bool Enabled { get; init; }
    public string[] ClientIds { get; init; } = [];
    public TimeSpan FlowLifetime { get; init; } = TimeSpan.FromMinutes(10);

    public void Validate()
    {
        if (Enabled && !ClientIds.Any(clientId => !string.IsNullOrWhiteSpace(clientId)))
        {
            throw new InvalidOperationException("At least one Google client id is required when Google authentication is enabled.");
        }

        if (FlowLifetime < TimeSpan.FromMinutes(5) || FlowLifetime > TimeSpan.FromMinutes(10))
        {
            throw new InvalidOperationException("Google authentication flow lifetime must be between 5 and 10 minutes.");
        }
    }
}
