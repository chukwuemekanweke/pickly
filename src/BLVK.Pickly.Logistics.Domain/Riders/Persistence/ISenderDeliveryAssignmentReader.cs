using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface ISenderDeliveryAssignmentReader
{
    Task<SenderAssignmentSnapshot> GetAsync(Guid deliveryId, CancellationToken cancellationToken);
}
