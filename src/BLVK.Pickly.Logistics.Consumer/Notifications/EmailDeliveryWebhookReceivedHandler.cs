using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;

namespace BLVK.Pickly.Logistics.Consumer.Notifications;

public sealed class EmailDeliveryWebhookReceivedHandler(
    IReadRepository<Provider> providerRepository,
    IRepository<EmailDeliveryWebhookInbox> emailDeliveryWebhookInboxRepository,
    IRepository<EmailNotificationLog> emailNotificationLogRepository,
    ICurrentActorAccessor currentActorAccessor,
    IMessageContext messageContext,
    ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IRepository<MessageInbox> messageInboxRepository,
    ILogger<EmailDeliveryWebhookReceivedHandler> logger) : BaseMessageHandler<EmailDeliveryWebhookReceived>(customTelemetryContext, currentActorAccessor, messageContext, messageInboxRepository, unitOfWork, timeProvider, logger)
{
    protected override async Task HandleAsyncInternal(EmailDeliveryWebhookReceived message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message.ProviderMessageId))
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid provider message id.");
        }

        if (message.ProviderId == Guid.Empty)
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid provider id.");
        }

        if (string.IsNullOrWhiteSpace(message.EventId))
        {
            throw new CannotProcessMessageNonTransientException(
                "EmailDeliveryWebhookReceived must contain a valid event id.");
        }

        var inbox = await emailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
            new EmailDeliveryWebhookInboxByEventIdSpecification(message.ProviderId, message.EventId),
            cancellationToken);
        if (inbox is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no webhook inbox was found for event '{message.EventId}'.");
        }

        if (inbox.WebhookProcessingStatus == WebhookProcessingStatus.Processed)
        {
            return;
        }

        var emailNotificationLog = await emailNotificationLogRepository.FirstOrDefaultAsync(
            new EmailNotificationLogByProviderMessageIdSpecification(message.ProviderMessageId),
            cancellationToken);
        if (emailNotificationLog is null)
        {
            throw new CannotProcessMessageNonTransientException(
                $"Unable to process EmailDeliveryWebhookReceived because no notification log was found for provider message '{message.ProviderMessageId}'.");
        }

        var now = Clock.GetUtcNow();
        var wasAlreadyDelivered = emailNotificationLog.DeliveredAtUtc.HasValue;
        emailNotificationLog.MarkDelivered(inbox.OccurredAtUtc);
        emailNotificationLogRepository.Update(emailNotificationLog);
        inbox.MarkProcessed(KnownWebhookStatusChangeReasons.Notifications.NotificationLogDelivered, now);
        emailDeliveryWebhookInboxRepository.Update(inbox);

        if (wasAlreadyDelivered)
        {
            return;
        }

        var provider = await providerRepository.FirstOrDefaultAsync(
            new ProviderByIdSpecification(message.ProviderId),
            cancellationToken);

        CustomTelemetryContext.AddCustomEvent(
            Observability.EventNames.Notifications.EmailDelivered,
            ObservabilityEventProperties.Create(
                ActorAccessor,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Common.MessageId] = emailNotificationLog.MessageId.ToString(),
                    [Observability.PropertyNames.Notifications.ProviderKey] = provider?.ProviderKey ?? string.Empty,
                    [Observability.PropertyNames.Notifications.ProviderMessageId] = emailNotificationLog.ProviderMessageId ?? message.ProviderMessageId,
                    [Observability.PropertyNames.Notifications.NotificationType] = emailNotificationLog.NotificationType.ToString()
                }));
    }
}

