using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Domain.Authentication.Specifications;

public sealed class DeviceAccountByIdentifierSpecification : Specification<DeviceAccount>
{
    public DeviceAccountByIdentifierSpecification(string identifier)
    {
        var normalizedIdentifier = DeviceAccount.NormalizeIdentifier(identifier);
        Where(deviceAccount => deviceAccount.Identifier == normalizedIdentifier);
        ApplyPaging(0, 1);
        EnableTracking();
    }
}
