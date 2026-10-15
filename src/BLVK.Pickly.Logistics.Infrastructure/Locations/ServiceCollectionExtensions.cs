using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var resolutionOptions = configuration.GetSection(LocationResolutionOptions.SectionName).Get<LocationResolutionOptions>()
            ?? new LocationResolutionOptions();
        resolutionOptions.Validate();
        var mapboxOptions = configuration.GetSection(MapboxGeocodingOptions.SectionName).Get<MapboxGeocodingOptions>()
            ?? new MapboxGeocodingOptions();
        mapboxOptions.Validate();

        services.Configure<LocationResolutionOptions>(configuration.GetSection(LocationResolutionOptions.SectionName));
        services.Configure<MapboxGeocodingOptions>(configuration.GetSection(MapboxGeocodingOptions.SectionName));
        services.AddTransient<GeocodingAttemptTelemetryHandler>();

        // The request URI carries the access token (Mapbox only accepts it as a query parameter), so the default
        // HttpClientFactory request loggers are removed and the client logs its own token-free messages.
        services.AddHttpClient<ILocationGeocodingProvider, MapboxGeocodingClient>(client =>
            {
                client.BaseAddress = new Uri(mapboxOptions.BaseUrl);
                client.Timeout = mapboxOptions.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(mapboxOptions.MaxRetries))
            .AddHttpMessageHandler<GeocodingAttemptTelemetryHandler>();

        return services;
    }
}
