namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

public sealed class DeliveryRoutingOptions
{
    public const string SectionName = "Deliveries:Routing:Mapbox";
    public string BaseUrl { get; init; } = "https://api.mapbox.com/";
    public string AccessToken { get; init; } = string.Empty;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(8);
    public int MaxRetries { get; init; } = 2;
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan DefaultRateLimitRetryAfter { get; init; } = TimeSpan.FromSeconds(30);

    public void Validate()
    {
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps) throw new InvalidOperationException("Deliveries:Routing:Mapbox:BaseUrl must be an absolute HTTPS URI.");
        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromSeconds(30)) throw new InvalidOperationException("Routing timeout must be between zero and 30 seconds.");
        if (MaxRetries is < 0 or > 3) throw new InvalidOperationException("Routing retries must be between zero and three.");
        if (CacheDuration <= TimeSpan.Zero || CacheDuration > TimeSpan.FromHours(1)) throw new InvalidOperationException("Routing cache duration must be between zero and one hour.");
    }
}
