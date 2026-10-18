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
        services.AddHttpClient<IDeliveryRouteProvider, MapboxDeliveryRouteProvider>(client => { client.BaseAddress = new Uri(options.BaseUrl); client.Timeout = options.Timeout; })
            .RemoveAllLoggers()
            .AddPolicyHandler(HttpRetryPolicies.CreateServerErrorRetryPolicy(options.MaxRetries));
        return services;
    }
}
