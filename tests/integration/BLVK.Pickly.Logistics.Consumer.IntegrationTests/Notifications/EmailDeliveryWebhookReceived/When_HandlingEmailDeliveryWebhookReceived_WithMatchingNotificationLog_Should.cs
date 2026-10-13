using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Chidelu.Integration.Messaging.RabbitMQ.Core;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher;
using Chidelu.Integration.Messaging.RabbitMQ.Publisher.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using EmailDeliveryWebhookReceivedEvent = BLVK.Pickly.Logistics.Contracts.Events.EmailDeliveryWebhookReceived;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Notifications.EmailDeliveryWebhookReceived;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingEmailDeliveryWebhookReceived_WithMatchingNotificationLog_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private readonly ContainersFixture _fixture = fixture;
    private Guid _messageId;
    private Guid _providerId;
    private Guid _webhookInboxId;
    private string _providerMessageId = string.Empty;
    private DateTimeOffset _deliveredAtUtc;

    protected override Task InitializeWorkerTestAsync() => SeedNotificationLogAsync();

    protected override Task DisposeWorkerTestAsync() => DeleteNotificationLogAsync();

    [Fact]
    public async Task MarkNotificationLogDelivered()
    {
        await WhenPublishingEmailDeliveryWebhookReceived();
        await ThenTheNotificationLogIsMarkedDelivered();

        async Task WhenPublishingEmailDeliveryWebhookReceived()
        {
            var publisherConfig = new PublisherConfig
            {
                ServiceName = "BLVK.Pickly.Logistics.Consumer.IntegrationTests.Publisher",
                HostName = _fixture.RabbitMqHostName,
                Port = _fixture.RabbitMqPort,
                UserName = _fixture.RabbitMqUserName,
                Password = _fixture.RabbitMqPassword,
                VirtualHost = _fixture.RabbitMqVirtualHost,
                EventsExchange = "x.events.blvkpicklylogistics.integrationtests"
            };

            await using var publisherServices = new ServiceCollection()
                .AddLogging()
                .AddPublisher(publisherConfig)
                .BuildServiceProvider();

            var publisher = publisherServices.GetRequiredKeyedService<IPublisher>(publisherConfig.Key);
            await publisher.PublishAsync(
                new EmailDeliveryWebhookReceivedEvent
                {
                    ProviderId = _providerId,
                    ProviderMessageId = _providerMessageId,
                    EventId = "evt_123"
                },
                CancellationToken.None,
                new Dictionary<string, string>
                {
                    [KnownMetadata.CorrelationId] = Guid.CreateVersion7().ToString("N")
                });
        }

        async Task ThenTheNotificationLogIsMarkedDelivered()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                var log = await scope.DbContext.EmailNotificationLogs
                    .FirstOrDefaultAsync(item => item.MessageId == _messageId);
                var inbox = await scope.DbContext.EmailDeliveryWebhookInboxes
                    .FirstOrDefaultAsync(item => item.Id == _webhookInboxId);

                return log?.DeliveredAtUtc == _deliveredAtUtc &&
                    inbox?.WebhookProcessingStatus == BLVK.Pickly.Logistics.Contracts.Payments.WebhookProcessingStatus.Processed;
            });

            using var assertionScope = CreateDbContextScope();
            var log = await assertionScope.DbContext.EmailNotificationLogs
                .FirstAsync(item => item.MessageId == _messageId);
            var inbox = await assertionScope.DbContext.EmailDeliveryWebhookInboxes
                .FirstAsync(item => item.Id == _webhookInboxId);

            log.ProviderMessageId.ShouldBe(_providerMessageId);
            log.DeliveredAtUtc.ShouldBe(_deliveredAtUtc);
            inbox.WebhookProcessingStatus.ShouldBe(BLVK.Pickly.Logistics.Contracts.Payments.WebhookProcessingStatus.Processed);
        }
    }

    private async Task SeedNotificationLogAsync()
    {
        _messageId = Guid.CreateVersion7();
        _providerId = Guid.CreateVersion7();
        _providerMessageId = $"mailtrap-{Guid.CreateVersion7():N}";
        _deliveredAtUtc = DateTimeOffset.Parse("2026-05-03T14:05:00+00:00");

        using var scope = CreateScope();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = EmailNotificationLog.Create(
            _messageId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            timeProvider.GetUtcNow());
        var inbox = EmailDeliveryWebhookInbox.Create(
            _providerId,
            "evt_123",
            _providerMessageId,
            "ada@example.com",
            "transactional",
            "mail.example.com",
            "{}",
            _deliveredAtUtc,
            timeProvider.GetUtcNow());

        log.MarkSent(_providerMessageId, timeProvider.GetUtcNow());
        await dbContext.EmailDeliveryWebhookInboxes.AddAsync(inbox);
        await dbContext.EmailNotificationLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();
        _webhookInboxId = inbox.Id;
    }

    private async Task DeleteNotificationLogAsync()
    {
        if (_messageId == Guid.Empty)
        {
            return;
        }

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inboxes = await dbContext.EmailDeliveryWebhookInboxes
            .Where(item => item.Id == _webhookInboxId)
            .ToListAsync();
        var logs = await dbContext.EmailNotificationLogs
            .Where(item => item.MessageId == _messageId)
            .ToListAsync();

        if (inboxes.Count == 0 && logs.Count == 0)
        {
            return;
        }

        if (inboxes.Count > 0)
        {
            dbContext.EmailDeliveryWebhookInboxes.RemoveRange(inboxes);
        }

        dbContext.EmailNotificationLogs.RemoveRange(logs);
        await dbContext.SaveChangesAsync();
    }
}
