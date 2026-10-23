namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

public sealed class LocationIqDeliveryRoutingOptions
{
    public const string SectionName = "Deliveries:Routing:LocationIQ";

    public required string BaseUrl { get; init; }
    public required string ApiKey { get; init; }
    public TimeSpan Timeout { get; init; }
    public int MaxRetries { get; init; }
    public TimeSpan DefaultRateLimitRetryAfter { get; init; }

    public void Validate() => DeliveryProviderOptionsValidator.Validate(
        SectionName,
        BaseUrl,
        Timeout,
        MaxRetries,
        DefaultRateLimitRetryAfter);
}
