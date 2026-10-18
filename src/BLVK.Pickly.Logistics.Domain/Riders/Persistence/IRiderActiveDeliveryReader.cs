using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderActiveDeliveryReader
{
    Task<RiderAssignedDeliverySnapshot?> GetAsync(Guid riderStakeholderId, CancellationToken cancellationToken);
}
