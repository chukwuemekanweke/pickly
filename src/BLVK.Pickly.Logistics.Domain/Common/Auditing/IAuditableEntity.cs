namespace BLVK.Pickly.Logistics.Domain.Common.Auditing;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }
    string? CreatedBy { get; }
    DateTimeOffset UpdatedAtUtc { get; }
    string? UpdatedBy { get; }

    void SetCreatedAudit(DateTimeOffset utcNow, string actorId);
    void SetUpdatedAudit(DateTimeOffset utcNow, string actorId);
}
