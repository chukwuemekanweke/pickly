using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Infrastructure.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDeliveryServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDeliveryTrackingIdGenerator, DeliveryTrackingIdGenerator>();
        var options = configuration.GetSection(DeliveryRoutingOptions.SectionName).Get<DeliveryRoutingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{DeliveryRoutingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<DeliveryRoutingOptions>(configuration.GetSection(DeliveryRoutingOptions.SectionName));

        var selectionSection = configuration.GetSection(DeliveryRouteProviderSelectionOptions.SectionName);
        if (!selectionSection.GetSection(nameof(DeliveryRouteProviderSelectionOptions.Providers)).Exists())
        {
            AddLegacyMapboxClient(services, options);
            return services;
        }

        var selectionOptions = selectionSection.Get<DeliveryRouteProviderSelectionOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{DeliveryRouteProviderSelectionOptions.SectionName}' is required.");

        var knownProviders = new HashSet<string>(StringComparer.Ordinal)
        {
            GeoapifyDeliveryRouteProvider.Provider,
            LocationIqDeliveryRouteProvider.Provider,
            MapboxDeliveryRouteProvider.Provider
        };
        selectionOptions.Validate(knownProviders);
        services.Configure<DeliveryRouteProviderSelectionOptions>(selectionSection);
        foreach (var provider in selectionOptions.Providers.Select(DeliveryRouteProviderSelectionOptions.Normalize))
        {
            switch (provider)
            {
                case GeoapifyDeliveryRouteProvider.Provider:
                    AddGeoapifyClient(services, configuration);
                    break;
                case LocationIqDeliveryRouteProvider.Provider:
                    AddLocationIqClient(services, configuration);
                    break;
                case MapboxDeliveryRouteProvider.Provider:
                    AddMapboxClient(services, options);
                    break;
            }
        }

        services.AddTransient<IDeliveryRouteProvider, RoundRobinDeliveryRouteProvider>();
        return services;
    }

    private static void AddGeoapifyClient(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GeoapifyDeliveryRoutingOptions.SectionName);
        var options = section.Get<GeoapifyDeliveryRoutingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{GeoapifyDeliveryRoutingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<GeoapifyDeliveryRoutingOptions>(section);
        services.AddHttpClient<GeoapifyDeliveryRouteProvider>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        services.AddTransient<IRoutingProviderClient>(provider => provider.GetRequiredService<GeoapifyDeliveryRouteProvider>());
    }

    private static void AddLocationIqClient(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(LocationIqDeliveryRoutingOptions.SectionName);
        var options = section.Get<LocationIqDeliveryRoutingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{LocationIqDeliveryRoutingOptions.SectionName}' is required.");
        options.Validate();
        services.Configure<LocationIqDeliveryRoutingOptions>(section);
        services.AddHttpClient<LocationIqDeliveryRouteProvider>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        services.AddTransient<IRoutingProviderClient>(provider => provider.GetRequiredService<LocationIqDeliveryRouteProvider>());
    }

    private static void AddMapboxClient(IServiceCollection services, DeliveryRoutingOptions options)
    {
        services.AddHttpClient<MapboxDeliveryRouteProvider>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        services.AddTransient<IRoutingProviderClient>(provider => provider.GetRequiredService<MapboxDeliveryRouteProvider>());
    }

    private static void AddLegacyMapboxClient(IServiceCollection services, DeliveryRoutingOptions options)
    {
        services.AddHttpClient<IDeliveryRouteProvider, MapboxDeliveryRouteProvider>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = options.Timeout;
            })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
    }
}
