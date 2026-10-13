using BLVK.Pickly.Logistics.Contracts.Common;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Domain.Common.Entities;

public abstract class Entity : IAuditableEntity, ISoftDelete
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public DateTimeOffset UpdatedAtUtc { get; protected set; }
    public string? UpdatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAtUtc { get; protected set; }
    public string? DeletedBy { get; protected set; }

    public void SetCreatedAudit(DateTimeOffset utcNow, string actorId)
    {
        CreatedAtUtc = utcNow;
        CreatedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

    public void SetUpdatedAudit(DateTimeOffset utcNow, string actorId)
    {
        UpdatedAtUtc = utcNow;
        UpdatedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

    public void SetDeletedAudit(DateTimeOffset utcNow, string actorId)
    {
        IsDeleted = true;
        DeletedAtUtc = utcNow;
        DeletedBy = string.IsNullOrWhiteSpace(actorId) ? ActorDefaults.SystemActorId : actorId;
    }

}
