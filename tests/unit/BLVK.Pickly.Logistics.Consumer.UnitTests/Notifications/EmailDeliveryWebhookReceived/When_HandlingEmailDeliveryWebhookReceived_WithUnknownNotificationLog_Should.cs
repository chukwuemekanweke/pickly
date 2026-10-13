using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BLVK.Pickly.Logistics.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithUnknownNotificationLog_Should
{
    [Fact]
    public async Task ThrowCannotProcessMessageNonTransientException()
    {
        var context = new NotificationsConsumerTestContext();

        context.EmailDeliveryWebhookInboxRepository.FirstOrDefaultAsync(
                Arg.Any<BLVK.Pickly.Logistics.Domain.Notifications.Specifications.EmailDeliveryWebhookInboxByEventIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns(EmailDeliveryWebhookInbox.Create(
                Guid.CreateVersion7(),
                "evt_123",
                "mailtrap-message-id",
                "ada@example.com",
                "transactional",
                "mail.example.com",
                "{}",
                DateTimeOffset.Parse("2026-05-03T13:05:00+00:00"),
                context.Clock.GetUtcNow()));
        context.EmailNotificationLogRepository.FirstOrDefaultAsync(
                Arg.Any<BLVK.Pickly.Logistics.Domain.Notifications.Specifications.EmailNotificationLogByProviderMessageIdSpecification>(),
                Arg.Any<CancellationToken>())
            .Returns((EmailNotificationLog?)null);

        var exception = await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
                new EmailDeliveryWebhookReceivedEvent
                {
                    ProviderId = Guid.CreateVersion7(),
                    ProviderMessageId = "mailtrap-message-id",
                    EventId = "evt_123"
                },
                CancellationToken.None));

        exception.Message.ShouldContain("mailtrap-message-id");
        context.EmailNotificationLogRepository.DidNotReceive().Update(Arg.Any<EmailNotificationLog>());
        await context.UnitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
