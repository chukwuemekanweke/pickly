using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.Specifications;

/// <summary>
/// Returns at most two matches so callers can tell a unique full-address identity apart from an ambiguous one.
/// </summary>
public sealed class UsableLocationsByNormalizedAddressSpecification : Specification<Location>
{
    public UsableLocationsByNormalizedAddressSpecification(string normalizedAddress)
    {
        Where(location =>
            location.ResolutionStatus == LocationResolutionStatus.Resolved &&
            location.Address.NormalizedAddress == normalizedAddress);
        ApplyOrderBy(location => location.CreatedAtUtc);
        ApplyPaging(0, 2);
    }
}
