using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Stakeholders.Specifications;

public sealed class StakeholderTypeByTenantAndKeySpecification : Specification<StakeholderType>
{
    public StakeholderTypeByTenantAndKeySpecification(Guid tenantId, string key)
    {
        var normalizedKey = key.Trim();
        Where(stakeholderType =>
            stakeholderType.TenantId == tenantId &&
            stakeholderType.Key == normalizedKey);
    }
}
