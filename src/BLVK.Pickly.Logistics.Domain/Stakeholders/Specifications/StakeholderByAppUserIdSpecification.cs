using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Stakeholders.Specifications;

public sealed class StakeholderByAppUserIdSpecification : Specification<Stakeholder>
{
    public StakeholderByAppUserIdSpecification(Guid appUserId)
    {
        Where(stakeholder => stakeholder.AppUserId == appUserId);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
