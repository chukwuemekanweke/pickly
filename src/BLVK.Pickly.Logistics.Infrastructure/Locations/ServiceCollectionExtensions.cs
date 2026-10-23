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
            ?? throw new InvalidOperationException($"Configuration section '{LocationResolutionOptions.SectionName}' is required.");
        resolutionOptions.Validate();
        services.Configure<LocationResolutionOptions>(configuration.GetSection(LocationResolutionOptions.SectionName));
        var selectionSection = configuration.GetSection(GeocodingProviderSelectionOptions.SectionName);
        var selectionOptions = selectionSection.Get<GeocodingProviderSelectionOptions>();
        if (selectionOptions is null)
        {
            AddLegacyMapboxClient(services, configuration);
            return services;
        }

        var knownProviders = new HashSet<string>(StringComparer.Ordinal)
        {
            GeoapifyGeocodingClient.Provider,
            LocationIqGeocodingClient.Provider,
            MapboxGeocodingClient.Provider
        };
        selectionOptions.Validate(knownProviders);
        services.Configure<GeocodingProviderSelectionOptions>(selectionSection);

        foreach (var provider in selectionOptions.Providers.Select(GeocodingProviderSelectionOptions.Normalize))
        {
            switch (provider)
            {
                case GeoapifyGeocodingClient.Provider:
                    AddGeoapifyClient(services, configuration);
                    break;
                case LocationIqGeocodingClient.Provider:
                    AddLocationIqClient(services, configuration);
                    break;
                case MapboxGeocodingClient.Provider:
                    AddMapboxClient(services, configuration);
                    break;
            }
        }

        services.AddTransient<ILocationGeocodingProvider, RoundRobinGeocodingProvider>();

        return services;
    }

    private static void AddGeoapifyClient(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GeoapifyGeocodingOptions.SectionName);
        var options = section.Get<GeoapifyGeocodingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{GeoapifyGeocodingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<GeoapifyGeocodingOptions>(section);
        services.AddHttpClient<GeoapifyGeocodingClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        services.AddTransient<IGeocodingProviderClient>(provider => provider.GetRequiredService<GeoapifyGeocodingClient>());
    }

    private static void AddLocationIqClient(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(LocationIqGeocodingOptions.SectionName);
        var options = section.Get<LocationIqGeocodingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{LocationIqGeocodingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<LocationIqGeocodingOptions>(section);
        services.AddHttpClient<LocationIqGeocodingClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        services.AddTransient<IGeocodingProviderClient>(provider => provider.GetRequiredService<LocationIqGeocodingClient>());
    }

    private static void AddMapboxClient(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MapboxGeocodingOptions.SectionName);
        var options = section.Get<MapboxGeocodingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{MapboxGeocodingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<MapboxGeocodingOptions>(section);
        services.AddHttpClient<MapboxGeocodingClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        services.AddTransient<IGeocodingProviderClient>(provider => provider.GetRequiredService<MapboxGeocodingClient>());
    }

    private static void AddLegacyMapboxClient(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(MapboxGeocodingOptions.SectionName);
        var options = section.Get<MapboxGeocodingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{MapboxGeocodingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<MapboxGeocodingOptions>(section);
        services.AddHttpClient<ILocationGeocodingProvider, MapboxGeocodingClient>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
    }
}
