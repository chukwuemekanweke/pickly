namespace BLVK.Pickly.Logistics.Domain.Common.Auditing;

public interface ICurrentActor
{
    string ActorId { get; }
    Guid? TenantId { get; }
    string CorrelationId { get; }
    string FlowId { get; }
}
