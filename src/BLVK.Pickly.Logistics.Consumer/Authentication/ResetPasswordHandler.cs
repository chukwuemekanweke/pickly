using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
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

public sealed class ResetPasswordHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ITwoFactorOtpService twoFactorOtpService,
    IStakeholderReadModelRepository stakeholderReadModelRepository,
    ICommandSender commandSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<ResetPasswordHandler> logger) : BaseMessageHandler<ResetPasswordCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(ResetPasswordCommand message, CancellationToken cancellationToken)
    {
        if (!message.StakeholderId.HasValue)
        {
            throw new CannotProcessMessageNonTransientException("ResetPasswordCommand must contain a valid stakeholder id.");
        }

        var stakeholder = await stakeholderReadModelRepository.GetByStakeholderIdAsync(message.StakeholderId.Value, cancellationToken);
        if (stakeholder is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process ResetPasswordCommand because no stakeholder could be found for stakeholder '{message.StakeholderId}'.");
        }

        if (await twoFactorOtpService.OtpExistsAsync(stakeholder.AppUserId, OtpIntent.PasswordReset, cancellationToken))
        {
            return;
        }

        var otp = await twoFactorOtpService.GenerateOtpAsync(
            stakeholder.AppUserId,
            OtpIntent.PasswordReset,
            cancellationToken,
            characterLength: 6,
            isAlphaNumeric: false);

        await commandSender.SendAsync(
            new SendNotificationCommand(
                stakeholder.TenantId,
                stakeholder.CountryId,
                NotificationType.ResetPasswordOtp,
                NotificationMedium.Email,
                new EmailNotificationContent(
                    stakeholder.EmailAddress,
                    new Dictionary<string, string>
                    {
                        ["FirstName"] = stakeholder.FirstName,
                        ["LastName"] = stakeholder.LastName,
                        ["OtpCode"] = otp.Code,
                        ["OtpExpiresAtUtc"] = DateTimeFormatter.FormatHumanReadableUtc(otp.ExpiresAtUtc, Clock.GetUtcNow()),
                        ["IllustrationUrl"] = NotificationIllustrationUrls.ResetPassword
                    }))
            {
                StakeholderId = stakeholder.StakeholderId,
                Language = stakeholder.Language
            },
            cancellationToken);
        CustomTelemetryContext.SetProperty(Observability.PropertyNames.Common.StakeholderId, stakeholder.StakeholderId.ToString());
        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetOtpSent,
            ObservabilityEventProperties.Create(CurrentActorAccessor, stakeholder.StakeholderId));
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(ResetPasswordCommand message)
    {
        yield break;
    }
}
