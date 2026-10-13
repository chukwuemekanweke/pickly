using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BLVK.Pickly.Logistics.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithAlreadyProcessedWebhook_Should
{
    [Fact]
    public async Task ReturnWithoutUpdatingNotificationLog()
    {
        var context = new NotificationsConsumerTestContext();
        var providerId = Guid.CreateVersion7();
        var inbox = EmailDeliveryWebhookInbox.Create(
            providerId,
            "evt_123",
            "mailtrap-message-id",
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            DateTimeOffset.Parse("2026-05-03T13:01:00+00:00"),
            context.Clock.GetUtcNow());

        inbox.MarkProcessed(KnownWebhookStatusChangeReasons.Notifications.NotificationLogDelivered, context.Clock.GetUtcNow());
        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<EmailDeliveryWebhookInboxByEventIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(inbox);

        await context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = "mailtrap-message-id",
                EventId = "evt_123"
            },
            CancellationToken.None);

        await context.EmailNotificationLogRepository.DidNotReceive()
            .FirstOrDefaultAsync(Arg.Any<EmailNotificationLogByProviderMessageIdSpecification>(), Arg.Any<CancellationToken>());
        context.EmailNotificationLogRepository.DidNotReceive().Update(Arg.Any<EmailNotificationLog>());
        context.EmailDeliveryWebhookInboxRepository.DidNotReceive().Update(Arg.Any<EmailDeliveryWebhookInbox>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        inbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
    }
}
