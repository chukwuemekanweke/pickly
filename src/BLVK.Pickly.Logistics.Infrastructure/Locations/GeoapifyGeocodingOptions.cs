namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

public sealed class GeoapifyGeocodingOptions
{
    public const string SectionName = "Locations:Geoapify";

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
