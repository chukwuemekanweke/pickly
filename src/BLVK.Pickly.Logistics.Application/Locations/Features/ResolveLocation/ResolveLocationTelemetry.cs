using BLVK.Pickly.Logistics.Domain.Common.Observability;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed class ResolveLocationTelemetry(ICustomTelemetryContext telemetry)
{
    public void Record(
        ResolveLocationCommand request,
        string eventName,
        Dictionary<string, string>? properties = null)
    {
        var additional = properties ?? [];
        additional[Observability.PropertyNames.Locations.CountryCode] = request.CountryCode;
        telemetry.AddCustomEvent(
            eventName,
            ObservabilityEventProperties.Create(
                request.ActorContext,
                request.ActorContext.StakeholderId,
                additionalProperties: additional));
    }
}
