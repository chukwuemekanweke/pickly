using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.Specifications;

public sealed class LocationByResolutionKeySpecification : Specification<Location>
{
    public LocationByResolutionKeySpecification(string resolutionKey)
    {
        Where(location =>
            location.ResolutionStatus == LocationResolutionStatus.Resolved &&
            location.ResolutionKeys.Any(key => key.Key == resolutionKey));
    }
}
