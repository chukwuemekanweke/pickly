using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Specifications;

public sealed class IpAddressByValueSpecification : Specification<IpAddress>
{
    public IpAddressByValueSpecification(string value)
    {
        Where(ipAddress => ipAddress.Value == value);
        AddInclude(ipAddress => ipAddress.Locations);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
