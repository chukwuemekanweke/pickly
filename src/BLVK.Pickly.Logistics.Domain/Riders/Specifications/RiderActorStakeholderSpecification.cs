using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Specifications;

public sealed class RiderActorStakeholderSpecification : Specification<Stakeholder>
{
    public RiderActorStakeholderSpecification(Guid stakeholderId)
    {
        Where(stakeholder => stakeholder.Id == stakeholderId);
        AddInclude(stakeholder => stakeholder.StakeholderType);
    }
}
