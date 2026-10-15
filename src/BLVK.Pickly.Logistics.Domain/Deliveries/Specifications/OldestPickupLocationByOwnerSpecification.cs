using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class OldestPickupLocationByOwnerSpecification : Specification<SavedPickupLocation>
{
    public OldestPickupLocationByOwnerSpecification(Guid senderId, bool enableTracking = false)
    {
        Where(location => location.SenderId == senderId);
        ApplyOrderBy(location => location.CreatedAtUtc);
        ApplyThenBy(location => location.Id);
        ApplyPaging(0, 1);

        if (enableTracking)
        {
            EnableTracking();
        }
    }
}
