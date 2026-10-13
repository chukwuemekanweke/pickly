using BLVK.Pickly.Logistics.Domain.Common.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Infrastructure.Storage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ObjectStorageOptions>(configuration.GetSection(ObjectStorageOptions.SectionName));
        services.Configure<CloudflareR2Options>(configuration.GetSection(CloudflareR2Options.SectionName));

        services.AddScoped<IObjectStorageService, ObjectStorageService>();
        services.AddSingleton<ICloudflareR2ClientFactory, CloudflareR2ClientFactory>();
        services.AddScoped<IObjectStorageProvider, NoopObjectStorageProvider>();
        services.AddScoped<IObjectStorageProvider, CloudflareR2ObjectStorageProvider>();

        return services;
    }
}
