using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed class ResolveLocationTelemetry(ICustomTelemetryContext telemetry)
{
    public void Record(
        ResolveLocationCommand request,
        string eventName,
        Dictionary<string, string>? properties = null)
        => Record(request.ActorContext, request.CountryCode, eventName, properties);

    public void Record(
        ActorContext actorContext,
        string countryCode,
        string eventName,
        Dictionary<string, string>? properties = null)
    {
        var additional = properties ?? [];
        additional[Observability.PropertyNames.Locations.CountryCode] = countryCode;
        telemetry.AddCustomEvent(
            eventName,
            ObservabilityEventProperties.Create(
                actorContext,
                actorContext.StakeholderId,
                additionalProperties: additional));
    }
}
