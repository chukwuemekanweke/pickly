using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderActiveDeliveryReader(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderActiveDeliveryReader
{
    public async Task<RiderActiveDeliveryResult> GetAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var delivery = await dbContext.Deliveries
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item =>
                item.RiderId == resolved.Profile!.StakeholderId &&
                RiderPersistenceSupport.ActiveDeliveryStatuses.Contains(item.Status),
                cancellationToken);
        return delivery is null
            ? new(RiderOperationCode.NotFound)
            : new(RiderOperationCode.Success, RiderPersistenceSupport.CreateAssignedDeliverySnapshot(delivery));
    }
}
