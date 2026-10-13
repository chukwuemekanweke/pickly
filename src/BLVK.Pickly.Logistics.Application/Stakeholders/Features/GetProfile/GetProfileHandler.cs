using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;

public sealed class GetProfileHandler(IStakeholderReadModelRepository stakeholderReadModelRepository)
{
    public async Task<GetProfileResult> HandleAsync(
        GetProfileQuery query,
        CancellationToken cancellationToken)
    {
        var stakeholderId = query.ActorContext.StakeholderId;
        var tenantId = query.ActorContext.TenantId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty ||
            !tenantId.HasValue || tenantId.Value == Guid.Empty)
        {
            return new GetProfileResult(GetProfileStatus.NotAuthenticated);
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(
            stakeholderId.Value,
            cancellationToken);

        if (stakeholder is null || stakeholder.TenantId != tenantId.Value)
        {
            return new GetProfileResult(GetProfileStatus.StakeholderNotFound);
        }

        return new GetProfileResult(
            GetProfileStatus.Success,
            new GetProfileResponse(
                stakeholder.StakeholderId,
                stakeholder.EmailAddress,
                stakeholder.FirstName,
                stakeholder.LastName,
                stakeholder.AvatarUrl,
                stakeholder.IsVerified));
    }
}
