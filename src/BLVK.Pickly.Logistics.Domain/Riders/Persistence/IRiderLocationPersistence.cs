using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderLocationPersistence
{
    Task<RiderOperationResult> UpdateAsync(
        ActorContext actor,
        decimal latitude,
        decimal longitude,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);
}
