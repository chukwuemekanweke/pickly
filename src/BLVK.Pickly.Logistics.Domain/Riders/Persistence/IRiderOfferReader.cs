using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferReader
{
    Task<RiderOffersResult> ListAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
