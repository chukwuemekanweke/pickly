using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Appends a <see cref="RiderAvailabilityHistory"/> row in the same save (and therefore the same transaction)
/// whenever a tracked <see cref="RiderProfile"/> is added or its persisted availability changes, so every
/// availability transition path is captured without each handler repeating it.
/// </summary>
internal sealed class RiderAvailabilityHistoryInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AppendHistory(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AppendHistory(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendHistory(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var changes = new List<RiderAvailabilityHistory>();

        // Entries<T>() runs DetectChanges, so snapshot-tracked property modifications are visible here.
        foreach (var entry in context.ChangeTracker.Entries<RiderProfile>())
        {
            if (entry.State == EntityState.Added)
            {
                changes.Add(RiderAvailabilityHistory.Create(entry.Entity.Id, null, entry.Entity.AvailabilityStatus, now));
                continue;
            }

            if (entry.State != EntityState.Modified)
            {
                continue;
            }

            var property = entry.Property(profile => profile.AvailabilityStatus);
            var previous = (RiderAvailabilityStatus)property.OriginalValue;
            if (property.IsModified && previous != entry.Entity.AvailabilityStatus)
            {
                changes.Add(RiderAvailabilityHistory.Create(entry.Entity.Id, previous, entry.Entity.AvailabilityStatus, now));
            }
        }

        if (changes.Count > 0)
        {
            context.AddRange(changes);
        }
    }
}
