using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public sealed class UserCreatedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    EmailConfirmationOtpSender emailConfirmationOtpSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UserCreatedHandler> logger,
    IRepository<MessageInbox> messageInboxRepository) : BaseMessageHandler<UserCreated>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserCreated message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("UserCreated must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserCreated because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        var user = await identityService.FindByIdAsync(stakeholder.AppUserId);
        if (user is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process UserCreated because no user could be found for stakeholder '{message.StakeholderId}'.");
        }

        if (user.EmailConfirmed)
        {
            logger.LogWarning(
                "Skipping sign-up OTP delivery for email {EmailAddress} because the email is already confirmed.",
                stakeholder.EmailAddress);

            AddOtpSendSkippedEvent(
                stakeholder.StakeholderId,
                message,
                Clock.GetUtcNow(),
                ObservabilityFailureReasons.AlreadyConfirmed);

            return;
        }

        var processedAtUtc = Clock.GetUtcNow();
        var sendStatus = await emailConfirmationOtpSender.SendAsync(
            stakeholder,
            message.ExpiresAtUtc,
            cancellationToken);
        if (sendStatus is not EmailConfirmationOtpSendStatus.Sent)
        {
            var failureReason = sendStatus == EmailConfirmationOtpSendStatus.ActiveOtpExists
                ? ObservabilityFailureReasons.ActiveOtpExists
                : ObservabilityFailureReasons.InsufficientOtpLifetime;
            AddOtpSendSkippedEvent(stakeholder.StakeholderId, message, processedAtUtc, failureReason);

            if (sendStatus == EmailConfirmationOtpSendStatus.InsufficientLifetime)
            {
                logger.LogWarning(
                    "Skipping sign-up OTP delivery for stakeholder {StakeholderId} because only {RemainingLifetimeMilliseconds}ms remains.",
                    stakeholder.StakeholderId,
                    (message.ExpiresAtUtc - processedAtUtc).TotalMilliseconds);
            }

            return;
        }

        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSent,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                stakeholder.StakeholderId,
                additionalProperties: EmailConfirmationOtpTelemetryProperties.Create(
                    message.RequestedAtUtc,
                    message.ExpiresAtUtc,
                    processedAtUtc)));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserCreated message)
    {
        yield break;
    }

    private void AddOtpSendSkippedEvent(
        Guid stakeholderId,
        UserCreated message,
        DateTimeOffset processedAtUtc,
        string reason)
    {
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholderId.ToString());
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.FailureReason, reason);
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.EmailConfirmationOtpSendSkipped,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                stakeholderId,
                reason,
                EmailConfirmationOtpTelemetryProperties.Create(
                    message.RequestedAtUtc,
                    message.ExpiresAtUtc,
                    processedAtUtc)));
    }
}
