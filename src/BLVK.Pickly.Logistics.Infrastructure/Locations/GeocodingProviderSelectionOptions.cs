namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

public sealed class GeocodingProviderSelectionOptions
{
    public const string SectionName = "Locations:Geocoding";

    public required IReadOnlyList<string> Providers { get; init; }
    public TimeSpan MaximumRateLimitCooldown { get; init; }

    public void Validate(IReadOnlySet<string> knownProviders)
    {
        if (Providers is not { Count: > 0 })
        {
            throw new InvalidOperationException("At least one geocoding provider must be enabled.");
        }

        var normalized = Providers.Select(Normalize).ToArray();
        if (normalized.Any(provider => !knownProviders.Contains(provider)))
        {
            throw new InvalidOperationException("Locations:Geocoding:Providers contains an unknown provider.");
        }

        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
        {
            throw new InvalidOperationException("Locations:Geocoding:Providers must not contain duplicates.");
        }

        if (MaximumRateLimitCooldown <= TimeSpan.Zero || MaximumRateLimitCooldown > TimeSpan.FromHours(1))
        {
            throw new InvalidOperationException("The maximum geocoding rate-limit cooldown must be positive and no more than one hour.");
        }
    }

    internal static string Normalize(string provider) => provider.Trim().ToLowerInvariant();
}
