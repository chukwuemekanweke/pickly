using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class SavedPickupLocationByOwnerAndIdSpecification : Specification<SavedPickupLocation>
{
    public SavedPickupLocationByOwnerAndIdSpecification(
        Guid senderId,
        Guid locationId,
        bool enableTracking = false)
    {
        Where(location =>
            location.Id == locationId &&
            location.SenderId == senderId);

        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
