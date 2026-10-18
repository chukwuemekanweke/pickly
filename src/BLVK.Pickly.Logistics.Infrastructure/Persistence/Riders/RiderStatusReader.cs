using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderStatusReader(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderStatusReader
{
    public async Task<RiderStatusResult> GetAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var location = await dbContext.RiderCurrentLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.RiderProfileId == resolved.Profile!.Id, cancellationToken);
        return await support.BuildStatusResultAsync(resolved.Profile!, location, utcNow, options, cancellationToken);
    }
}
