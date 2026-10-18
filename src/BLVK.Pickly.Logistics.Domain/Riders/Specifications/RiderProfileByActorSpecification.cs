using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Specifications;

public sealed class RiderProfileByActorSpecification : Specification<RiderProfile>
{
    public RiderProfileByActorSpecification(Guid stakeholderId, Guid tenantId, bool enableTracking)
    {
        Where(profile => profile.StakeholderId == stakeholderId && profile.TenantId == tenantId);
        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
