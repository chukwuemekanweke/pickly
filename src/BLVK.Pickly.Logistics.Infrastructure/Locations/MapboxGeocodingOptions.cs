namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

public sealed class MapboxGeocodingOptions
{
    public const string SectionName = "Locations:Mapbox";

    public string BaseUrl { get; init; } = "https://api.mapbox.com/";
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Explicit operator confirmation that the Mapbox account and commercial terms permit Geocoding v6 permanent
    /// storage. Resolution refuses to run without it rather than falling back to temporary results.
    /// </summary>
    public bool PermanentStorageEnabled { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(8);
    public int MaxRetries { get; init; } = 2;
    public TimeSpan DefaultRateLimitRetryAfter { get; init; } = TimeSpan.FromSeconds(30);

    public bool IsPermanentGeocodingAvailable =>
        PermanentStorageEnabled && !string.IsNullOrWhiteSpace(AccessToken);

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
