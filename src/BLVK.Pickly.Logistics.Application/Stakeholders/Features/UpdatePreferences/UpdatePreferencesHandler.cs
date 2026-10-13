using BLVK.Pickly.Logistics.Domain.Common.Localization;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdatePreferences;

public sealed class UpdatePreferencesHandler(
    IRepository<Stakeholder> stakeholderRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdatePreferencesResult> HandleAsync(
        UpdatePreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        var tenantId = command.ActorContext.TenantId;
        if (!stakeholderId.HasValue || !tenantId.HasValue)
        {
            return new UpdatePreferencesResult(UpdatePreferencesStatus.NotAuthenticated);
        }

        if (!StakeholderThemes.All.Contains(command.Theme, StringComparer.OrdinalIgnoreCase) ||
            !SupportedLanguages.IsSupported(command.Language))
        {
            return new UpdatePreferencesResult(UpdatePreferencesStatus.ValidationFailed);
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId.Value, cancellationToken);
        if (stakeholder is null || stakeholder.TenantId != tenantId.Value)
        {
            return new UpdatePreferencesResult(UpdatePreferencesStatus.StakeholderNotFound);
        }

        stakeholder.UpdatePreferences(command.Theme, command.Language);
        stakeholderRepository.Update(stakeholder);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdatePreferencesResult(UpdatePreferencesStatus.Success);
    }
}
