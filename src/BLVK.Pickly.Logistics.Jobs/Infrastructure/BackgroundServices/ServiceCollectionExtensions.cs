using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundServiceReadinessTracking(this IServiceCollection services)
    {
        services.TryAddSingleton<BackgroundServiceReadinessState>();
        return services;
    }
}
