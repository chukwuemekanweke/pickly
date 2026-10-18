using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferReader
{
    Task<IReadOnlyList<RiderDeliveryOffer>> ListExpiredAsync(
        Guid riderProfileId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<RiderOfferSnapshot>> ListActiveSnapshotsAsync(
        Guid riderProfileId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
