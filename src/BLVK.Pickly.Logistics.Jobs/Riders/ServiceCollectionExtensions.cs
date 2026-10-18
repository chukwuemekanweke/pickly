using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;

namespace BLVK.Pickly.Logistics.Jobs.Riders;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRiderMatching(this IServiceCollection services, IConfiguration configuration)
    {
        var matchingOptions = configuration.GetSection(RiderMatchingOptions.SectionName).Get<RiderMatchingOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RiderMatchingOptions.SectionName}' is required.");
        matchingOptions.Validate();
        services.Configure<RiderMatchingOptions>(configuration.GetSection(RiderMatchingOptions.SectionName));

        var processorOptions = configuration.GetSection(RiderMatchingProcessorOptions.SectionName).Get<RiderMatchingProcessorOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{RiderMatchingProcessorOptions.SectionName}' is required.");
        processorOptions.Validate();
        services.Configure<RiderMatchingProcessorOptions>(configuration.GetSection(RiderMatchingProcessorOptions.SectionName));
        services.AddSingleton(new BackgroundServiceDescriptor(RiderMatchingProcessor.ServiceName));
        services.AddHostedService<RiderMatchingProcessor>();
        return services;
    }
}
