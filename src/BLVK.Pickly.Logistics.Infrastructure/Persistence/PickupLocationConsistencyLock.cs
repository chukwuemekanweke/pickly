using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class PickupLocationConsistencyLock(AppDbContext dbContext) : IPickupLocationConsistencyLock
{
    public async Task AcquireAsync(
        Guid senderId,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"pickup-location:{senderId:N}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))",
            cancellationToken);
    }
}
