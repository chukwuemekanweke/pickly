using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class TenantEmailBaseTemplateByTenantIdSpecification : Specification<TenantEmailBaseTemplate>
{
    public TenantEmailBaseTemplateByTenantIdSpecification(Guid tenantId)
    {
        Where(template => template.TenantId == tenantId);
    }
}
