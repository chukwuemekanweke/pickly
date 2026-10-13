using BLVK.Pickly.Logistics.Contracts.Common;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

internal sealed class CurrentActorAccessor : ICurrentActorAccessor
{
    public string ActorId { get; private set; } = ActorDefaults.SystemActorId;
    public Guid? TenantId { get; private set; }
    public string CorrelationId { get; private set; } = Activity.Current?.Id ?? Guid.CreateVersion7().ToString("N");
    public string FlowId { get; private set; } = Guid.CreateVersion7().ToString("N");

    public void Set(string actorId, Guid? tenantId, string correlationId, string flowId)
    {
        ActorId = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
        TenantId = tenantId;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? Guid.CreateVersion7().ToString("N") : correlationId;
        FlowId = flowId ?? string.Empty;
    }
}
