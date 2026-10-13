namespace BLVK.Pickly.Logistics.Domain.Common.Auditing;

public interface ICurrentActorAccessor : ICurrentActor
{
    void Set(string actorId, Guid? tenantId, string correlationId, string flowId);
}
