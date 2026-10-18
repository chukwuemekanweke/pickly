using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderConsistencyLock(AppDbContext dbContext) : IRiderConsistencyLock
{
    public Task AcquireRiderAsync(Guid riderStakeholderId, CancellationToken cancellationToken) =>
        AcquireKeyAsync(RiderKey(riderStakeholderId), cancellationToken);

    public Task AcquireDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        AcquireKeyAsync(DeliveryKey(deliveryId), cancellationToken);

    public Task<bool> TryAcquireDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken)
    {
        var key = DeliveryKey(deliveryId);
        return dbContext.Database
            .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock(hashtextextended({key}, 0)) AS \"Value\"")
            .SingleAsync(cancellationToken);
    }

    public Task AcquireAssignmentAsync(
        Guid deliveryId,
        Guid riderStakeholderId,
        CancellationToken cancellationToken) =>
        AcquireDeliveriesThenRiderAsync([deliveryId], riderStakeholderId, cancellationToken);

    public async Task AcquireDeliveriesThenRiderAsync(
        IReadOnlyCollection<Guid> deliveryIds,
        Guid riderStakeholderId,
        CancellationToken cancellationToken)
    {
        foreach (var deliveryId in deliveryIds.Distinct().Order())
        {
            await AcquireDeliveryAsync(deliveryId, cancellationToken);
        }

        await AcquireRiderAsync(riderStakeholderId, cancellationToken);
    }

    private static string RiderKey(Guid riderStakeholderId) => $"rider:{riderStakeholderId:D}";

    private static string DeliveryKey(Guid deliveryId) => $"delivery:{deliveryId:D}";

    private Task<int> AcquireKeyAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            cancellationToken);
}
