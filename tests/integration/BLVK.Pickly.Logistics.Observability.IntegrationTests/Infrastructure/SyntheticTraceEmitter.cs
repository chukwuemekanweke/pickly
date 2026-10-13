using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.Observability.IntegrationTests.Infrastructure;

/// <summary>
/// Emits one synthetic OTLP trace directly to a collector endpoint, so tail-sampling policies can be
/// exercised without standing up a fake production API just to generate traces.
/// </summary>
public static class SyntheticTraceEmitter
{
    private const string SourceName = "BLVK.Pickly.Logistics.Observability.IntegrationTests";

    public static async Task<string> EmitAsync(string otlpEndpoint, bool isError = false, TimeSpan? duration = null)
    {
        using var activitySource = new ActivitySource(SourceName);
        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .ConfigureResource(resource => resource.AddService("synthetic-trace-emitter"))
            .AddSource(SourceName)
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            })
            .Build();

        string traceId;
        using (var activity = activitySource.StartActivity("synthetic-operation", ActivityKind.Server))
        {
            if (activity is null)
            {
                throw new InvalidOperationException("No listener was registered for the synthetic trace activity source.");
            }

            if (duration.HasValue)
            {
                await Task.Delay(duration.Value);
            }

            if (isError)
            {
                activity.SetStatus(ActivityStatusCode.Error, "synthetic failure");
            }

            traceId = activity.TraceId.ToHexString();
        }

        tracerProvider.ForceFlush(5_000);

        return traceId;
    }
}
