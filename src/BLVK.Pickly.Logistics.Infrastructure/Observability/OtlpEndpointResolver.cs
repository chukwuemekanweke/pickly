namespace BLVK.Pickly.Logistics.Infrastructure.Observability;

public static class OtlpEndpointResolver
{
    public static Uri Resolve(string endpoint, string? protocol, string signal)
    {
        if (!string.Equals(protocol, "http/protobuf", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(endpoint);
        }

        var endpointUri = new Uri(endpoint);
        var signalPath = $"/v1/{signal}";
        if (endpointUri.AbsolutePath.EndsWith(signalPath, StringComparison.OrdinalIgnoreCase))
        {
            return endpointUri;
        }

        var builder = new UriBuilder(endpointUri)
        {
            Path = $"{endpointUri.AbsolutePath.TrimEnd('/')}{signalPath}"
        };

        return builder.Uri;
    }
}
