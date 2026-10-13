using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Formatting;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public sealed class UserSignInFailedHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IAuthenticationIdentityService identityService,
    IAuthenticationSessionService sessionService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    ILogger<UserSignInFailedHandler> logger,
    IRepository<MessageInbox> messageInboxRepository) : BaseMessageHandler<UserSignInFailed>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(UserSignInFailed message, CancellationToken cancellationToken)
    {
        var user = await identityService.FindByEmailAsync(message.EmailAddress);
        if (user is null)
        {
            logger.LogInformation(
                "Skipping sign-in failure processing because no user could be resolved for email {EmailAddress}.",
                message.EmailAddress);

            return;
        }

        if (message.FailureReason == UserSignInFailureReasons.InvalidCredentials)
        {
            var accessFailedResult = await identityService.AccessFailedAsync(user);
            if (!accessFailedResult.Succeeded)
            {
                throw new InvalidOperationException($"Failed to increment access-failed count for user {user.Id}.");
            }
        }

        StakeholderReadModel? stakeholder = null;
        if (message.StakeholderId.HasValue)
        {
            stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        }
        if (stakeholder is not null)
        {
            CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        }

        if (message.FailureReason == UserSignInFailureReasons.InvalidCredentials &&
            await identityService.IsLockedOutAsync(user))
        {
            if (stakeholder is null)
            {
                throw new CannotProcessMessageNonTransientException(
                    $"Unable to process UserSignInFailed because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
            }

            await sessionService.RevokeAllAsync(stakeholder.StakeholderId, cancellationToken);

            var lockedUntilUtc = await identityService.GetLockoutEndUtcAsync(user)
                ?? throw new InvalidOperationException($"User {user.Id} is locked out but has no lockout end time.");

            await commandSender.SendAsync(
                new SendNotificationCommand(
                    stakeholder.TenantId,
                    stakeholder.CountryId,
                    NotificationType.AccountLocked,
                    NotificationMedium.Email,
                    new EmailNotificationContent(
                        user.Email ?? message.EmailAddress,
                        new Dictionary<string, string>
                        {
                            ["LockedUntilUtc"] = DateTimeFormatter.FormatHumanReadableUtc(lockedUntilUtc, Clock.GetUtcNow())
                        }))
                {
                    StakeholderId = stakeholder.StakeholderId,
                    Language = stakeholder.Language
                },
                cancellationToken);
        }

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.SignInFailureProcessed,
            ObservabilityEventProperties.Create(
                CurrentActorAccessor,
                stakeholder?.StakeholderId,
                message.FailureReason));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(UserSignInFailed message)
    {
        yield break;
    }
}
