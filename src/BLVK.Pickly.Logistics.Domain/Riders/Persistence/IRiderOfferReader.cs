using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferReader
{
    Task<IReadOnlyCollection<RiderOfferSnapshot>> ListActiveSnapshotsAsync(
        Guid riderProfileId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}
