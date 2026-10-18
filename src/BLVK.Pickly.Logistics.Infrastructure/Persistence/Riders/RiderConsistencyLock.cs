using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderConsistencyLock(AppDbContext dbContext) : IRiderConsistencyLock
{
    private const string MatchingProcessorLockKey = "rider-matching-processor";

    public Task AcquireRiderAsync(Guid riderStakeholderId, CancellationToken cancellationToken) =>
        AcquireKeyAsync($"rider:{riderStakeholderId:D}", cancellationToken);

    public async Task AcquireAssignmentAsync(
        Guid deliveryId,
        Guid riderStakeholderId,
        CancellationToken cancellationToken)
    {
        var keys = new[] { $"delivery:{deliveryId:D}", $"rider:{riderStakeholderId:D}" }
            .OrderBy(value => value, StringComparer.Ordinal);
        foreach (var key in keys)
        {
            await AcquireKeyAsync(key, cancellationToken);
        }
    }

    public Task AcquireMatchingProcessorAsync(CancellationToken cancellationToken) =>
        AcquireKeyAsync(MatchingProcessorLockKey, cancellationToken);

    private Task<int> AcquireKeyAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            cancellationToken);
}
