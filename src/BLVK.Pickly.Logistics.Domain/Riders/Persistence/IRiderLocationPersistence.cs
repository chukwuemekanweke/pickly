using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderLocationPersistence
{
    Task<RiderCurrentLocation?> FindAsync(Guid riderProfileId, CancellationToken cancellationToken);

    void TrackPosition(
        RiderCurrentLocation location,
        decimal latitude,
        decimal longitude,
        bool isNew);
}
