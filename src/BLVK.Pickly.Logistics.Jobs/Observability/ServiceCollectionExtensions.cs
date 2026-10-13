using BLVK.Pickly.Logistics.Infrastructure.Observability;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BLVK.Pickly.Logistics.Jobs.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJobsOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var otlpProtocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];

        services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("BLVK.Pickly.Logistics.Jobs"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(Domain.Common.Observability.Observability.ActivitySourceName)
                    .AddSource("RabbitMQ.Client.Publisher")
                    .AddSource("RabbitMQ.Client.Subscriber")
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    tracing.AddOtlpExporter(options => options.Endpoint = OtlpEndpointResolver.Resolve(otlpEndpoint, otlpProtocol, "traces"));
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    metrics.AddOtlpExporter(options => options.Endpoint = OtlpEndpointResolver.Resolve(otlpEndpoint, otlpProtocol, "metrics"));
                }
            });

        var serviceName = configuration["OpenTelemetry:ServiceName"] ?? "BLVK.Pickly.Logistics.Jobs";
        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            services.AddLogging(logging =>
            {
                logging.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = false;
                    options.ParseStateValues = true;
                    options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
                    options.AddOtlpExporter(exporterOptions =>
                        exporterOptions.Endpoint = OtlpEndpointResolver.Resolve(otlpEndpoint, otlpProtocol, "logs"));
                });
            });
        }

        return services;
    }
}
