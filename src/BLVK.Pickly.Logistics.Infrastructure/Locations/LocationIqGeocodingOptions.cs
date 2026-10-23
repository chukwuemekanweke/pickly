namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

public sealed class LocationIqGeocodingOptions
{
    public const string SectionName = "Locations:LocationIQ";

    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public TimeSpan Timeout { get; init; }
    public int MaxRetries { get; init; }
    public TimeSpan DefaultRateLimitRetryAfter { get; init; }

    public void Validate() => ProviderOptionsValidator.Validate(
        SectionName,
        BaseUrl,
        Timeout,
        MaxRetries,
        DefaultRateLimitRetryAfter);
}
