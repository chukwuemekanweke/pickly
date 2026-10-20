using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using System.Globalization;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed class ResolveLocationTelemetry(ICustomTelemetryContext telemetry)
{
    public void Record(
        ResolveLocationCommand request,
        string eventName,
        Dictionary<string, string>? properties = null)
    {
        var searchProperties = CreateSearchProperties(
            string.Join(", ", new[] { request.AddressLine, request.City, request.State }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            "exact_resolution");
        Merge(searchProperties, properties);
        Record(request.ActorContext, request.CountryCode, eventName, searchProperties);
    }

    public Dictionary<string, string> CreateSearchProperties(string searchTerm, string origin)
    {
        var normalized = LocationSearchTerms.NormalizeForComparison(searchTerm);
        return new Dictionary<string, string>
        {
            [Observability.PropertyNames.Locations.SearchTerm] = searchTerm.Trim(),
            [Observability.PropertyNames.Locations.NormalizedSearchTerm] = normalized,
            [Observability.PropertyNames.Locations.SearchTermCount] = CountTerms(normalized)
                .ToString(CultureInfo.InvariantCulture),
            [Observability.PropertyNames.Locations.ResolutionOrigin] = origin
        };
    }

    public void Record(
        ActorContext actorContext,
        string countryCode,
        string eventName,
        Dictionary<string, string>? properties = null)
    {
        var additional = properties is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(properties);
        additional[Observability.PropertyNames.Locations.CountryCode] = countryCode;
        telemetry.AddCustomEvent(
            eventName,
            ObservabilityEventProperties.Create(
                actorContext,
                actorContext.StakeholderId,
                additionalProperties: additional));
    }

    private static int CountTerms(string value) =>
        value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static void Merge(Dictionary<string, string> target, IReadOnlyDictionary<string, string>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var property in source)
        {
            target[property.Key] = property.Value;
        }
    }
}
