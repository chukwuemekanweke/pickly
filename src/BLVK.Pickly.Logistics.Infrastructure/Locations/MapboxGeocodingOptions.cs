namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

public sealed class MapboxGeocodingOptions
{
    public const string SectionName = "Locations:Mapbox";

    public string BaseUrl { get; init; } = null!;
    public string AccessToken { get; init; } = null!;

    public TimeSpan Timeout { get; init; }
    public int MaxRetries { get; init; }
    public TimeSpan DefaultRateLimitRetryAfter { get; init; }

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Locations:Mapbox:BaseUrl must be an absolute HTTPS URI.");
        }

        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromSeconds(30))
        {
            throw new InvalidOperationException("Locations:Mapbox:Timeout must be greater than zero and at most 30 seconds.");
        }

        if (MaxRetries is < 0 or > 3)
        {
            throw new InvalidOperationException("Locations:Mapbox:MaxRetries must be between 0 and 3.");
        }

        if (DefaultRateLimitRetryAfter <= TimeSpan.Zero || DefaultRateLimitRetryAfter > TimeSpan.FromMinutes(5))
        {
            throw new InvalidOperationException("Locations:Mapbox:DefaultRateLimitRetryAfter must be between zero and 5 minutes.");
        }
    }
}
