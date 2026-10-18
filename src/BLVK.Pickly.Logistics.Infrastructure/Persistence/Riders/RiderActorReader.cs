using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderActorReader(AppDbContext dbContext) : IRiderActorReader
{
    private const string RiderStakeholderTypeKey = "rider";

    public async Task<RiderProfile?> FindAsync(
        Guid stakeholderId,
        Guid? tenantId,
        bool trackProfile,
        CancellationToken cancellationToken)
    {
        var stakeholder = await dbContext.Stakeholders
            .AsNoTracking()
            .Include(item => item.StakeholderType)
            .SingleOrDefaultAsync(item => item.Id == stakeholderId, cancellationToken);
        if (stakeholder is null || !stakeholder.IsVerified ||
            !string.Equals(stakeholder.StakeholderType.Key, RiderStakeholderTypeKey, StringComparison.OrdinalIgnoreCase) ||
            (tenantId.HasValue && tenantId != stakeholder.TenantId))
        {
            return null;
        }

        IQueryable<RiderProfile> query = dbContext.RiderProfiles;
        if (!trackProfile)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            item => item.StakeholderId == stakeholder.Id && item.TenantId == stakeholder.TenantId,
            cancellationToken);
    }
}
