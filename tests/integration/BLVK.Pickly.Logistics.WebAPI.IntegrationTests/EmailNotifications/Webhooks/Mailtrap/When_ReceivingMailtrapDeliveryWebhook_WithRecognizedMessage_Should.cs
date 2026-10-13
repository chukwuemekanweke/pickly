using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.EmailNotifications.Webhooks.Mailtrap;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReceivingMailtrapDeliveryWebhook_WithRecognizedMessage_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(
        fixture,
        new Dictionary<string, string?>
        {
            ["Notifications:Email:Mailtrap:WebhookSigningSecret"] = "test-signing-secret"
        }),
        IAsyncLifetime
{
    private Guid _providerId;
    private Guid _emailNotificationLogId;
    private Guid _webhookInboxId;
    private Guid _outboxMessageId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await SeedNotificationSetupAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task PersistWebhook()
    {
        await WhenPostingWebhook();
        await ThenTheWebhookIsPersisted();

        async Task WhenPostingWebhook()
        {
            var payload = new
            {
                events = new[]
                {
                    new
                    {
                        @event = "delivery",
                        message_id = "mailtrap-message-id",
                        sending_stream = "transactional",
                        email = "ada@example.com",
                        sending_domain_name = "mail.example.com",
                        timestamp = DateTimeOffset.Parse("2026-05-03T12:05:00+00:00").ToUnixTimeSeconds(),
                        event_id = "evt_123"
                    }
                }
            };

            var rawPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl.EmailNotificationWebhooks.Mailtrap.V1)
            {
                Content = new StringContent(rawPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Mailtrap-Signature", ComputeSignature("test-signing-secret", rawPayload));

            _response = await Client.SendAsync(request);
        }

        async Task ThenTheWebhookIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
            var inbox = await dbContext.EmailDeliveryWebhookInboxes
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstAsync(item => item.ProviderId == _providerId);
            var log = await dbContext.EmailNotificationLogs.FirstAsync(item => item.Id == _emailNotificationLogId);
            var outboxMessage = await dbContext.OutboxMessages
                .OrderByDescending(item => item.EnqueuedAtUtc)
                .FirstAsync(item =>
                    item.Kind == OutboxMessageKind.Event &&
                    item.Type == typeof(EmailDeliveryWebhookReceived).FullName!);

            _webhookInboxId = inbox.Id;
            _outboxMessageId = outboxMessage.Id;
            inbox.ProviderMessageId.ShouldBe("mailtrap-message-id");
            inbox.WebhookEventId.ShouldBe("evt_123");
            inbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Received);
            log.DeliveredAtUtc.ShouldBeNull();
            outboxMessage.SentAtUtc.ShouldBeNull();
        }
    }

    private async Task SeedNotificationSetupAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var provider = Provider.Create(ProviderType.Email, "Mailtrap", "mailtrap", true);
        var log = EmailNotificationLog.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            NotificationType.SignInSuccessful,
            [],
            "ada@example.com",
            null,
            null,
            now);
        log.MarkSent("mailtrap-message-id", now);

        await dbContext.Providers.AddAsync(provider);
        await dbContext.EmailNotificationLogs.AddAsync(log);
        await dbContext.SaveChangesAsync();

        _providerId = provider.Id;
        _emailNotificationLogId = log.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();

        if (_webhookInboxId != Guid.Empty)
        {
            var inbox = await dbContext.EmailDeliveryWebhookInboxes.FirstOrDefaultAsync(item => item.Id == _webhookInboxId);
            if (inbox is not null)
            {
                dbContext.EmailDeliveryWebhookInboxes.Remove(inbox);
            }
        }

        if (_outboxMessageId != Guid.Empty)
        {
            var outboxMessage = await dbContext.OutboxMessages.FirstOrDefaultAsync(item => item.Id == _outboxMessageId);
            if (outboxMessage is not null)
            {
                dbContext.OutboxMessages.Remove(outboxMessage);
            }
        }

        var log = await dbContext.EmailNotificationLogs.FirstOrDefaultAsync(item => item.Id == _emailNotificationLogId);
        if (log is not null)
        {
            dbContext.EmailNotificationLogs.Remove(log);
        }

        var provider = await dbContext.Providers.FirstOrDefaultAsync(item => item.Id == _providerId);
        if (provider is not null)
        {
            dbContext.Providers.Remove(provider);
        }

        await dbContext.SaveChangesAsync();
    }

    private static string ComputeSignature(string signingSecret, string rawPayload)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret), Encoding.UTF8.GetBytes(rawPayload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}


