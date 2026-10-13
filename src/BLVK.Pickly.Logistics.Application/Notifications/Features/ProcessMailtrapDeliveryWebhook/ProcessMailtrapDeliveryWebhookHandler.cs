using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using BLVK.Pickly.Logistics.Domain.Notifications.Specifications;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed class ProcessMailtrapDeliveryWebhookHandler(
    IReadRepository<Provider> providerRepository,
    IRepository<EmailDeliveryWebhookInbox> emailDeliveryWebhookInboxRepository,
    IMailtrapWebhookSignatureValidator mailtrapWebhookSignatureValidator,
    IEventPublisher eventPublisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<ProcessMailtrapDeliveryWebhookResult> HandleAsync(
        ProcessMailtrapDeliveryWebhookCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await mailtrapWebhookSignatureValidator.ValidateAsync(
            new MailtrapWebhookSignatureValidationRequest(command.SignatureHeader, command.RawPayload),
            cancellationToken);
        if (!validationResult.IsValid)
        {
            return new ProcessMailtrapDeliveryWebhookResult(
                MailtrapDeliveryWebhookReceiptStatus.InvalidSignature,
                validationResult.StatusChangeReason);
        }

        var provider = await providerRepository.FirstOrDefaultAsync(
                new ProviderByTypeAndKeySpecification(ProviderType.Email, EmailProviderKeys.Mailtrap),
                cancellationToken)
            ?? throw new InvalidOperationException("Mailtrap email provider is not configured.");

        var now = timeProvider.GetUtcNow();
        var hasPersistedWebhook = false;
        var webhookEventIds = command.Events
            .Select(item => item.EventId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var existingWebhooks = await emailDeliveryWebhookInboxRepository.ListAsync(
            new EmailDeliveryWebhookInboxesByEventIdsSpecification(provider.Id, webhookEventIds),
            cancellationToken);
        var existingWebhookEventIds = existingWebhooks
            .Select(item => item.WebhookEventId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var webhookEvent in command.Events)
        {
            if (!existingWebhookEventIds.Add(webhookEvent.EventId))
            {
                continue;
            }

            var rawEventPayload = System.Text.Json.JsonSerializer.Serialize(webhookEvent);
            var occurredAtUtc = DateTimeOffset.FromUnixTimeSeconds(webhookEvent.Timestamp);
            var inbox = EmailDeliveryWebhookInbox.Create(
                provider.Id,
                webhookEvent.EventId,
                webhookEvent.MessageId,
                webhookEvent.Email,
                webhookEvent.SendingStream,
                webhookEvent.SendingDomainName,
                rawEventPayload,
                occurredAtUtc,
                now);
            await emailDeliveryWebhookInboxRepository.AddAsync(inbox, cancellationToken);
            await eventPublisher.PublishAsync(
                new EmailDeliveryWebhookReceived
                {
                    ProviderId = provider.Id,
                    EventId = webhookEvent.EventId,
                    ProviderMessageId = webhookEvent.MessageId,
                    OccuredAt = occurredAtUtc
                },
                cancellationToken);

            hasPersistedWebhook = true;
        }

        if (!hasPersistedWebhook)
        {
            return new ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus.Duplicate);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessMailtrapDeliveryWebhookResult(MailtrapDeliveryWebhookReceiptStatus.Persisted);
    }
}
