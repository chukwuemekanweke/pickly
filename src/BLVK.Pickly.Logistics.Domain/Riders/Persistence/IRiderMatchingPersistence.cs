using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderMatchingPersistence
{
    Task<MatchingProcessingResult> ProcessAsync(
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);
}
