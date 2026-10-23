namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal static class DeliveryProviderOptionsValidator
{
    public static void Validate(
        string sectionName,
        string baseUrl,
        TimeSpan timeout,
        int maxRetries,
        TimeSpan defaultRateLimitRetryAfter)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException($"{sectionName}:BaseUrl must be an absolute HTTPS URI.");
        }

        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(30))
        {
            throw new InvalidOperationException($"{sectionName}:Timeout must be greater than zero and at most 30 seconds.");
        }

        if (maxRetries is < 0 or > 3)
        {
            throw new InvalidOperationException($"{sectionName}:MaxRetries must be between 0 and 3.");
        }

        if (defaultRateLimitRetryAfter <= TimeSpan.Zero || defaultRateLimitRetryAfter > TimeSpan.FromMinutes(5))
        {
            throw new InvalidOperationException($"{sectionName}:DefaultRateLimitRetryAfter must be positive and at most 5 minutes.");
        }
    }
}
