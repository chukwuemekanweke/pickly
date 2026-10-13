using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Authentication;

public sealed class TwoFactorActorResolver(
    IStakeholderReadModelRepository stakeholderRepository,
    IAuthenticationIdentityService identityService)
{
    public async Task<TwoFactorActor?> ResolveAsync(ActorContext actorContext, CancellationToken cancellationToken)
    {
        if (!actorContext.StakeholderId.HasValue || !actorContext.TenantId.HasValue)
            return null;

        var stakeholder = await stakeholderRepository.GetByStakeholderIdAsync(
            actorContext.StakeholderId.Value,
            cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != actorContext.TenantId.Value)
            return null;

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        return user is null ? null : new TwoFactorActor(user, stakeholder.StakeholderId);
    }
}
