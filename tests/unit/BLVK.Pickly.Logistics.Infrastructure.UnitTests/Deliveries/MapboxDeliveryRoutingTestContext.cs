using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

internal sealed class MapboxDeliveryRoutingTestContext
{
    public List<HttpRequestMessage> Requests { get; } = [];
    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = _ => Json("""{"code":"Ok","routes":[{"distance":8400.4,"duration":1200.5}]}""");

    public IDeliveryRouteProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Deliveries:Routing:Mapbox:BaseUrl"] = "https://api.mapbox.test/",
            ["Deliveries:Routing:Mapbox:AccessToken"] = "pk.test",
            ["Deliveries:Routing:Mapbox:MaxRetries"] = "0"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IJsonCache, MemoryJsonCache>();
        services.AddDeliveryServices(configuration);
        services.AddHttpClient<IDeliveryRouteProvider, MapboxDeliveryRouteProvider>().ConfigurePrimaryHttpMessageHandler(() => new StubHandler(this));
        return services.BuildServiceProvider().GetRequiredService<IDeliveryRouteProvider>();
    }

    public static DeliveryRouteRequest Request(Guid? originId = null, Guid? destinationId = null) => new(originId ?? Guid.CreateVersion7(), GeographicCoordinates.FromDegrees(6.45, 7.51), destinationId ?? Guid.CreateVersion7(), GeographicCoordinates.FromDegrees(6.46, 7.52), "driving");
    public static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class StubHandler(MapboxDeliveryRoutingTestContext context) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) { context.Requests.Add(request); return Task.FromResult(context.Responder(request)); }
    }

    private sealed class MemoryJsonCache : IJsonCache
    {
        private readonly Dictionary<string, object> _values = [];
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) => Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);
        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken) { _values[key] = value!; return Task.CompletedTask; }
        public Task RemoveAsync(string key, CancellationToken cancellationToken) { _values.Remove(key); return Task.CompletedTask; }
        public async Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken) { var value = await GetAsync<T>(key, cancellationToken); _values.Remove(key); return value; }
    }
}
