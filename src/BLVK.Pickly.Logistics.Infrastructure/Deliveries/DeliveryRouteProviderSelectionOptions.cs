namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

public sealed class DeliveryRouteProviderSelectionOptions
{
    public const string SectionName = "Deliveries:Routing";

    public required IReadOnlyList<string> Providers { get; init; }
    public TimeSpan MaximumRateLimitCooldown { get; init; }

    public void Validate(IReadOnlySet<string> knownProviders)
    {
        if (Providers is not { Count: > 0 })
        {
            throw new InvalidOperationException("At least one delivery routing provider must be enabled.");
        }

        var normalized = Providers.Select(Normalize).ToArray();
        if (normalized.Any(provider => !knownProviders.Contains(provider)))
        {
            throw new InvalidOperationException("Deliveries:Routing:Providers contains an unknown provider.");
        }

        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
        {
            throw new InvalidOperationException("Deliveries:Routing:Providers must not contain duplicates.");
        }

        if (MaximumRateLimitCooldown <= TimeSpan.Zero || MaximumRateLimitCooldown > TimeSpan.FromHours(1))
        {
            throw new InvalidOperationException("The maximum routing rate-limit cooldown must be positive and no more than one hour.");
        }
    }

    internal static string Normalize(string provider) => provider.Trim().ToLowerInvariant();
}
