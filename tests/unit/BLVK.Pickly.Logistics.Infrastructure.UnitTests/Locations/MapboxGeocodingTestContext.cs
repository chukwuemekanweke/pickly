using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Infrastructure.Locations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

/// <summary>
/// Builds the DI-configured Mapbox typed client (including its retry policy) over a stub primary handler, so no test
/// ever reaches the real, paid Mapbox API.
/// </summary>
internal sealed class MapboxGeocodingTestContext
{
    public const string AccessToken = "pk.test-token-value";

    public const string AddressFeatureCollection =
        """
        {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "id": "dXJuOm1ieGFkcjoxMg",
              "geometry": { "type": "Point", "coordinates": [7.5221, 6.4512] },
              "properties": {
                "mapbox_id": "dXJuOm1ieGFkcjoxMg",
                "feature_type": "address",
                "full_address": "12 Ekpulechi Avenue, Enugu 400001, Enugu, Nigeria",
                "name": "12 Ekpulechi Avenue",
                "name_preferred": "12 Ekpulechi Avenue",
                "place_formatted": "Enugu, Enugu, Nigeria",
                "coordinates": { "longitude": 7.5221, "latitude": 6.4512, "accuracy": "rooftop" },
                "context": {
                  "address": { "mapbox_id": "a", "address_number": "12", "street_name": "Ekpulechi Avenue", "name": "12 Ekpulechi Avenue" },
                  "street": { "mapbox_id": "s", "name": "Ekpulechi Avenue" },
                  "place": { "mapbox_id": "p", "name": "Enugu" },
                  "region": { "mapbox_id": "r", "name": "Enugu", "region_code": "EN" },
                  "country": { "mapbox_id": "c", "name": "Nigeria", "country_code": "NG", "country_code_alpha_3": "NGA" }
                },
                "match_code": { "address_number": "matched", "street": "matched", "place": "matched", "region": "matched", "country": "matched", "confidence": "exact" }
              }
            }
          ],
          "attribution": "test"
        }
        """;

    private readonly Dictionary<string, string?> _configuration = new()
    {
        ["Locations:Mapbox:BaseUrl"] = "https://api.mapbox.test/",
        ["Locations:Mapbox:AccessToken"] = AccessToken,
        ["Locations:Mapbox:PermanentStorageEnabled"] = "true",
        ["Locations:Mapbox:Timeout"] = "00:00:05",
        ["Locations:Mapbox:MaxRetries"] = "2"
    };

    public List<HttpRequestMessage> Requests { get; } = [];
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Responder { get; set; } =
        (_, _) => Task.FromResult(Json(AddressFeatureCollection));

    public MapboxGeocodingTestContext Configure(string key, string? value)
    {
        _configuration[$"Locations:Mapbox:{key}"] = value;
        return this;
    }

    public ILocationGeocodingProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(_configuration).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddLocationServices(configuration);
        services.AddHttpClient<ILocationGeocodingProvider, MapboxGeocodingClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(this));
        return services.BuildServiceProvider().GetRequiredService<ILocationGeocodingProvider>();
    }

    public static GeocodingRequest Request() => new("12 Ekpulechi Avenue", "Enugu", "Enugu", "NG", 5);

    public static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(MapboxGeocodingTestContext context) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            context.Requests.Add(request);
            return context.Responder(request, cancellationToken);
        }
    }
}
