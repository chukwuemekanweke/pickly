using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferAcceptancePersistence
{
    Task<RiderOfferDecisionResult> AcceptAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken);
}
