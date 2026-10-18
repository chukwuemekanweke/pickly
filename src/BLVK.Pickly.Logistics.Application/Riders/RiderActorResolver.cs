using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;

namespace BLVK.Pickly.Logistics.Application.Riders;

public sealed class RiderActorResolver(IRiderActorReader reader)
{
    public async Task<RiderActorResolution> ResolveAsync(
        ActorContext actor,
        bool trackProfile,
        CancellationToken cancellationToken)
    {
        if (!actor.StakeholderId.HasValue || actor.StakeholderId == Guid.Empty)
        {
            return new(RiderActorResolutionStatus.NotAuthenticated);
        }

        var profile = await reader.FindAsync(
            actor.StakeholderId.Value,
            actor.TenantId,
            trackProfile,
            cancellationToken);
        return profile is null
            ? new(RiderActorResolutionStatus.NotRider)
            : new(RiderActorResolutionStatus.Success, profile);
    }
}

public sealed record RiderActorResolution(
    RiderActorResolutionStatus Status,
    RiderProfile? Profile = null);

public enum RiderActorResolutionStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotRider = 3
}
