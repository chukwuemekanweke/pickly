using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Stakeholders.Specifications;

public sealed class TenantByIdSpecification : Specification<Tenant>
{
    public TenantByIdSpecification(Guid tenantId)
    {
        Where(tenant => tenant.Id == tenantId);
    }
}
