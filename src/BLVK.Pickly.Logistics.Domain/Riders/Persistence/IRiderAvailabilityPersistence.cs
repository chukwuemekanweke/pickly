using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderAvailabilityPersistence
{
    Task<RiderStatusResult> SetAsync(
        ActorContext actor,
        bool isAvailable,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);
}
