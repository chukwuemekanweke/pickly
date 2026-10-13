using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Domain.Common.Observability;

public static class ObservabilityEventProperties
{
    public static Dictionary<string, string> Create(
        ActorContext actorContext,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var properties = new Dictionary<string, string>
        {
            [Observability.PropertyNames.Common.CorrelationId] = string.IsNullOrWhiteSpace(actorContext.CorrelationId)
                ? Guid.CreateVersion7().ToString("N")
                : actorContext.CorrelationId,
            [Observability.PropertyNames.Common.FlowId] = actorContext.FlowId ?? string.Empty
        };

        if (actorContext.TenantId.HasValue)
        {
            properties[Observability.PropertyNames.Common.TenantId] = actorContext.TenantId.Value.ToString();
        }

        if (stakeholderId.HasValue)
        {
            properties[Observability.PropertyNames.Common.StakeholderId] = stakeholderId.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(failureReason))
        {
            properties[Observability.PropertyNames.Common.FailureReason] = failureReason;
        }

        if (additionalProperties is null)
        {
            return properties;
        }

        foreach (var property in additionalProperties)
        {
            properties[property.Key] = property.Value;
        }

        return properties;
    }

    public static Dictionary<string, string> Create(
        ICurrentActor currentActor,
        Guid? stakeholderId = null,
        string? failureReason = null,
        IReadOnlyDictionary<string, string>? additionalProperties = null)
    {
        var actorContext = ActorContext.FromAnonymousActor(currentActor);
        return Create(actorContext, stakeholderId, failureReason, additionalProperties);
    }
}
