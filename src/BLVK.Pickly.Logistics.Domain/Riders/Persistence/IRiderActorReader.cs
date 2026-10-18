using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderActorReader
{
    Task<RiderProfile?> FindAsync(
        Guid stakeholderId,
        Guid? tenantId,
        bool trackProfile,
        CancellationToken cancellationToken);
}
