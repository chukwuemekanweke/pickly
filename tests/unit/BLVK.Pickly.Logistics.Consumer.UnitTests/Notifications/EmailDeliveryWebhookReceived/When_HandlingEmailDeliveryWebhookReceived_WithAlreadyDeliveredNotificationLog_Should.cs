using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BLVK.Pickly.Logistics.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithAlreadyDeliveredNotificationLog_Should
{
    [Fact]
    public async Task IgnoreWebhook()
    {
        var context = new NotificationsConsumerTestContext();
        var providerId = Guid.CreateVersion7();
        var log = EmailNotificationLog.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            context.Clock.GetUtcNow());
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

        log.MarkSent("mailtrap-message-id", context.Clock.GetUtcNow());
        log.MarkDelivered(DateTimeOffset.Parse("2026-05-03T13:01:00+00:00"));
        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<EmailDeliveryWebhookInboxByEventIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(inbox);
        context.EmailNotificationLogRepository.FirstOrDefaultAsync(
                Arg.Any<EmailNotificationLogByProviderMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(log);

        await context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
            new EmailDeliveryWebhookReceivedEvent
            {
                ProviderId = providerId,
                ProviderMessageId = "mailtrap-message-id",
                EventId = "evt_123"
            },
            CancellationToken.None);

        log.DeliveredAtUtc.ShouldBe(DateTimeOffset.Parse("2026-05-03T13:01:00+00:00"));
        inbox.WebhookProcessingStatus.ShouldBe(BLVK.Pickly.Logistics.Contracts.Payments.WebhookProcessingStatus.Processed);
        context.EmailNotificationLogRepository.Received(1).Update(log);
        context.EmailDeliveryWebhookInboxRepository.Received(1).Update(inbox);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

