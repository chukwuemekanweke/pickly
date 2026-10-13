using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Application.Stakeholders.Features.UpdateProfile;

public sealed class UpdateProfileHandler(
    IRepository<Stakeholder> stakeholderRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<UpdateProfileResult> HandleAsync(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.NotAuthenticated);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.ProfileUpdateFailed,
                ObservabilityEventProperties.Create(command.ActorContext, failureReason: ObservabilityFailureReasons.NotAuthenticated));
            return new UpdateProfileResult(UpdateProfileStatus.NotAuthenticated);
        }

        if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName))
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.ValidationFailed);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.ProfileUpdateFailed,
                ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, ObservabilityFailureReasons.ValidationFailed));
            return new UpdateProfileResult(
                UpdateProfileStatus.ValidationFailed,
                "FirstName and LastName are required.");
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(stakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.Value.ToString());
            customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, ObservabilityFailureReasons.StakeholderNotFound);
            customTelemetryContext.AddCustomEvent(
                Observability.EventNames.Authentication.ProfileUpdateFailed,
                ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, ObservabilityFailureReasons.StakeholderNotFound));
            return new UpdateProfileResult(UpdateProfileStatus.StakeholderNotFound);
        }

        stakeholder.UpdateProfile(command.FirstName, command.LastName);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.ProfileUpdateCompleted,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId));

        return new UpdateProfileResult(UpdateProfileStatus.Success);
    }
}
