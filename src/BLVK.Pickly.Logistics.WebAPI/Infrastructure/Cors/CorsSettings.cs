namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.Cors;

public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; init; } = [];

    public void Validate()
    {
        if (AllowedOrigins.Length == 0)
        {
            throw new InvalidOperationException($"Configuration section '{SectionName}:AllowedOrigins' must contain at least one origin.");
        }

        foreach (var origin in AllowedOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"CORS origin '{origin}' must use the exact http(s) origin format 'scheme://host[:port]' without a path or trailing slash.");
            }
        }
    }
}
