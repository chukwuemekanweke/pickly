using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Notifications;

public sealed class SendNotificationHandler(
    ICustomTelemetryContext customTelemetryContext,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    IEmailNotificationService emailNotificationService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<SendNotificationHandler> logger) : BaseMessageHandler<SendNotificationCommand>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    public ICurrentActorAccessor CurrentActorAccessor { get; } = currentActorAccessor;

    protected override async Task HandleAsyncInternal(SendNotificationCommand message, CancellationToken cancellationToken)
    {
        try
        {
            switch (message.NotificationMedium)
            {
                case NotificationMedium.Email:
                    var sendResult = await emailNotificationService.SendAsync(message, cancellationToken);
                    if (sendResult is not null)
                    {
                        CustomTelemetryContext.AddCustomEvent(
                            Observability.EventNames.Notifications.EmailSent,
                            ObservabilityEventProperties.Create(
                                CurrentActorAccessor,
                                message.StakeholderId,
                                additionalProperties: new Dictionary<string, string>
                                {
                                    [Observability.PropertyNames.Common.MessageId] = message.MessageId.ToString(),
                                    [Observability.PropertyNames.Notifications.ProviderKey] = sendResult.ProviderKey,
                                    [Observability.PropertyNames.Notifications.ProviderMessageId] = sendResult.ProviderMessageId,
                                    [Observability.PropertyNames.Notifications.NotificationType] = message.NotificationType.ToString()
                                }));
                    }
                    break;

                default:
                    throw new FailedToProcessMessageException(
                        $"Notification medium '{message.NotificationMedium}' is not supported.");
            }
        }
        catch (NotificationConfigurationException exception)
        {
            throw new CannotProcessMessageNonTransientException(exception.Message);
        }
    }

    protected override IEnumerable<(string Key, string Value)> GetTelemetryParameters(SendNotificationCommand message)
    {
        yield break;
    }
}
