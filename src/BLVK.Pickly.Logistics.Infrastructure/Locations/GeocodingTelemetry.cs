using System.Diagnostics;
using System.Diagnostics.Metrics;
using ObservabilityNames = BLVK.Pickly.Logistics.Domain.Common.Observability.Observability;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

/// <summary>
/// Provider-call traces and metrics. Tags carry only provider and outcome, never addresses or tokens, to keep metric
/// cardinality bounded and customer data out of telemetry.
/// </summary>
internal static class GeocodingTelemetry
{
    public static readonly ActivitySource ActivitySource = new(ObservabilityNames.ActivitySourceName);

    private static readonly Meter Meter = new(ObservabilityNames.MeterName);

    private static readonly Counter<long> Requests = Meter.CreateCounter<long>(
        "pickly.locations.geocoding.requests",
        description: "Geocoding provider requests by provider and outcome.");

    private static readonly Histogram<double> Duration = Meter.CreateHistogram<double>(
        "pickly.locations.geocoding.duration",
        unit: "ms",
        description: "Geocoding provider request latency.");

    public static void RecordAttempt(string provider, string outcome, TimeSpan elapsed)
    {
        var tags = new TagList
        {
            { "provider", provider },
            { "outcome", outcome }
        };
        Requests.Add(1, tags);
        Duration.Record(elapsed.TotalMilliseconds, tags);
    }
}
