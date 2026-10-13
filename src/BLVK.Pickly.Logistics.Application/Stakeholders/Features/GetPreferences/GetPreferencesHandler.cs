using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetPreferences;

public sealed class GetPreferencesHandler(IStakeholderReadModelRepository stakeholderRepository)
{
    public async Task<GetPreferencesResult> HandleAsync(
        GetPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        var stakeholderId = query.ActorContext.StakeholderId;
        var tenantId = query.ActorContext.TenantId;
        if (!stakeholderId.HasValue || !tenantId.HasValue)
        {
            return new GetPreferencesResult(GetPreferencesStatus.NotAuthenticated);
        }

        var stakeholder = await stakeholderRepository.GetByStakeholderIdAsync(stakeholderId.Value, cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != tenantId.Value)
        {
            return new GetPreferencesResult(GetPreferencesStatus.StakeholderNotFound);
        }

        return new GetPreferencesResult(
            GetPreferencesStatus.Success,
            new GetPreferencesResponse(stakeholder.Theme, stakeholder.Language));
    }
}
