namespace BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record ActorContext(Guid? StakeholderId, Guid? TenantId, string CorrelationId, string FlowId)
{
    public static ActorContext FromCurrentActor(ICurrentActor currentActor)
    {
        if (!Guid.TryParse(currentActor.ActorId, out var stakeholderId))
        {
            throw new InvalidOperationException(
                $"Unable to resolve stakeholder id from actor id '{currentActor.ActorId}'.");
        }

        return new ActorContext(stakeholderId, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId);
    }

    public static ActorContext FromAnonymousActor(ICurrentActor currentActor) =>
        new(null, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId);
}
