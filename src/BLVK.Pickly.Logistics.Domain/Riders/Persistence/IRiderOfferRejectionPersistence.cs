using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferRejectionPersistence
{
    Task<RiderOperationResult> RejectAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);
}
