using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.AspNetCore.Identity;

namespace BLVK.Pickly.Logistics.Application.Authentication.Features.ChangePassword;

public sealed class ChangePasswordHandler(
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork)
{
    public async Task<ChangePasswordResult> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.ActorContext.StakeholderId.HasValue)
        {
            RecordFailure(command, null, ObservabilityFailureReasons.NotAuthenticated);
            return new ChangePasswordResult(ChangePasswordStatus.NotAuthenticated);
        }

        var stakeholderId = command.ActorContext.StakeholderId.Value;
        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(
            stakeholderId,
            cancellationToken);
        if (stakeholder is null)
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.StakeholderNotFound);
            return new ChangePasswordResult(ChangePasswordStatus.UserNotFound);
        }

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        if (user is null)
        {
            RecordFailure(command, stakeholderId, ObservabilityFailureReasons.UserNotFound);
            return new ChangePasswordResult(ChangePasswordStatus.UserNotFound);
        }

        var result = await identityService.ChangePasswordAsync(
            user,
            command.CurrentPassword,
            command.NewPassword);
        if (!result.Succeeded)
        {
            var incorrectCurrentPassword = result.Errors.Any(error =>
                error.Code == nameof(IdentityErrorDescriber.PasswordMismatch));
            RecordFailure(
                command,
                stakeholderId,
                incorrectCurrentPassword
                    ? ObservabilityFailureReasons.IncorrectCurrentPassword
                    : ObservabilityFailureReasons.ValidationFailed);

            return incorrectCurrentPassword
                ? new ChangePasswordResult(ChangePasswordStatus.IncorrectCurrentPassword)
                : new ChangePasswordResult(
                    ChangePasswordStatus.ValidationFailed,
                    result.ToValidationDictionary());
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordChangeCompleted,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId));

        return new ChangePasswordResult(ChangePasswordStatus.Success);
    }

    private void RecordFailure(
        ChangePasswordCommand command,
        Guid? stakeholderId,
        string failureReason)
    {
        customTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, failureReason);
        customTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordChangeFailed,
            ObservabilityEventProperties.Create(command.ActorContext, stakeholderId, failureReason));
    }
}
