using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BLVK.Pickly.Logistics.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Notifications.EmailDeliveryWebhookReceived;

public sealed class When_HandlingEmailDeliveryWebhookReceived_WithoutProviderId_Should
{
    [Fact]
    public async Task ThrowCannotProcessMessageNonTransientException()
    {
        var context = new NotificationsConsumerTestContext();

        await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            context.CreateEmailDeliveryWebhookReceivedHandler().HandleAsync(
                new EmailDeliveryWebhookReceivedEvent
                {
                    ProviderId = Guid.Empty,
                    ProviderMessageId = "mailtrap-message-id",
                    EventId = "evt_123"
                },
                CancellationToken.None));
    }
}
