using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Interceptors;

internal sealed class AuditAndSoftDeleteInterceptor(
    ICurrentActor currentActor,
    TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actorId = currentActor.ActorId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.SetCreatedAudit(now, actorId);
                }

                if (entry.State is EntityState.Added or EntityState.Modified)
                {
                    auditable.SetUpdatedAudit(now, actorId);
                }
            }

            if (entry.Entity is ISoftDelete softDelete && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDelete.SetDeletedAudit(now, actorId);

                if (entry.Entity is IAuditableEntity deletedAuditable)
                {
                    deletedAuditable.SetUpdatedAudit(now, actorId);
                }
            }
        }
    }
}
