using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderActiveDeliveryReader
{
    Task<RiderActiveDeliveryResult> GetAsync(
        ActorContext actor,
        CancellationToken cancellationToken);
}
