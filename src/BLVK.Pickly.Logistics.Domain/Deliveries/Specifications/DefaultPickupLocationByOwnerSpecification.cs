using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class DefaultPickupLocationByOwnerSpecification : Specification<SavedPickupLocation>
{
    public DefaultPickupLocationByOwnerSpecification(Guid senderId, bool enableTracking = false)
    {
        Where(location =>
            location.SenderId == senderId &&
            location.IsDefault);

        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
