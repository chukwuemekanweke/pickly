using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Specifications;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Riders;

public sealed class RiderActorResolver(
    IRepository<Stakeholder> stakeholders,
    IRepository<RiderProfile> profiles)
{
    private const string RiderStakeholderTypeKey = "rider";

    public async Task<RiderActorResolution> ResolveAsync(
        ActorContext actor,
        bool trackProfile,
        CancellationToken cancellationToken)
    {
        if (!actor.StakeholderId.HasValue || actor.StakeholderId == Guid.Empty)
        {
            return new(RiderActorResolutionStatus.NotAuthenticated);
        }

        var stakeholder = await stakeholders.FirstOrDefaultAsync(
            new RiderActorStakeholderSpecification(actor.StakeholderId.Value),
            cancellationToken);
        if (stakeholder is null || !stakeholder.IsVerified ||
            !string.Equals(stakeholder.StakeholderType.Key, RiderStakeholderTypeKey, StringComparison.OrdinalIgnoreCase) ||
            (actor.TenantId.HasValue && actor.TenantId != stakeholder.TenantId))
        {
            return new(RiderActorResolutionStatus.NotRider);
        }

        var profile = await profiles.FirstOrDefaultAsync(
            new RiderProfileByActorSpecification(stakeholder.Id, stakeholder.TenantId, trackProfile),
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
