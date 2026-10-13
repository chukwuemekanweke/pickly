using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace BLVK.Pickly.Logistics.Infrastructure.Observability;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomTelemetryContext(this IServiceCollection services)
    {
        services.AddSingleton<ICustomTelemetryContext, CustomTelemetryContext>();
        return services;
    }

    public static IServiceCollection AddBackendTelemetry(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceName = configuration["OpenTelemetry:ServiceName"]
            ?? throw new InvalidOperationException("Configuration value 'OpenTelemetry:ServiceName' is required.");
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var otlpProtocol = configuration["OTEL_EXPORTER_OTLP_PROTOCOL"];

        var telemetry = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName));

        telemetry.WithTracing(tracing =>
        {
            tracing
                .AddSource(Domain.Common.Observability.Observability.ActivitySourceName)
                .AddSource("RabbitMQ.Client.Publisher")
                .AddSource("RabbitMQ.Client.Subscriber")
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.Filter = ctx =>
                        !ctx.Request.Path.StartsWithSegments("/health")
                        && !ctx.Request.Path.StartsWithSegments("/metrics");
                })
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddRedisInstrumentation();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter(options => options.Endpoint = OtlpEndpointResolver.Resolve(otlpEndpoint, otlpProtocol, "traces"));
            }
        });

        telemetry.WithMetrics(metrics =>
        {
            metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter();

            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter(options => options.Endpoint = OtlpEndpointResolver.Resolve(otlpEndpoint, otlpProtocol, "metrics"));
            }
        });

        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = false;
                options.ParseStateValues = true;
                options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    options.AddOtlpExporter(exporterOptions =>
                        exporterOptions.Endpoint = OtlpEndpointResolver.Resolve(otlpEndpoint, otlpProtocol, "logs"));
                }
            });
        });

        return services;
    }
}
