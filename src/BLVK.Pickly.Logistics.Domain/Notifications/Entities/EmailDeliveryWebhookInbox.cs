using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Entities;

public sealed class EmailDeliveryWebhookInbox : Entity, IAggregateRoot
{
    private EmailDeliveryWebhookInbox()
    {
    }

    private EmailDeliveryWebhookInbox(
        Guid providerId,
        string webhookEventId,
        string providerMessageId,
        string recipientEmail,
        string sendingStream,
        string sendingDomainName,
        string rawPayload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset receivedAtUtc)
    {
        ProviderId = providerId;
        WebhookEventId = webhookEventId;
        ProviderMessageId = providerMessageId;
        RecipientEmail = recipientEmail;
        SendingStream = sendingStream;
        SendingDomainName = sendingDomainName;
        RawPayload = rawPayload;
        WebhookProcessingStatus = WebhookProcessingStatus.Received;
        OccurredAtUtc = occurredAtUtc;
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid ProviderId { get; private set; }
    public string WebhookEventId { get; private set; } = string.Empty;
    public string ProviderMessageId { get; private set; } = string.Empty;
    public string RecipientEmail { get; private set; } = string.Empty;
    public string SendingStream { get; private set; } = string.Empty;
    public string SendingDomainName { get; private set; } = string.Empty;
    public string RawPayload { get; private set; } = string.Empty;
    public WebhookProcessingStatus WebhookProcessingStatus { get; private set; }
    public string? StatusChangeReason { get; private set; }
    public string? ProcessingError { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static EmailDeliveryWebhookInbox Create(
        Guid providerId,
        string webhookEventId,
        string providerMessageId,
        string recipientEmail,
        string sendingStream,
        string sendingDomainName,
        string rawPayload,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset receivedAtUtc) =>
        new(
            providerId,
            NormalizeRequired(webhookEventId),
            NormalizeRequired(providerMessageId),
            NormalizeRequired(recipientEmail),
            NormalizeRequired(sendingStream),
            NormalizeRequired(sendingDomainName),
            NormalizeRequired(rawPayload),
            occurredAtUtc,
            receivedAtUtc);

    public void MarkProcessed(string? statusChangeReason, DateTimeOffset utcNow)
    {
        EnsureCanTransitionFrom(WebhookProcessingStatus.Received);
        WebhookProcessingStatus = WebhookProcessingStatus.Processed;
        StatusChangeReason = NormalizeOptional(statusChangeReason);
        ProcessingError = null;
        ProcessedAtUtc = utcNow;
    }

    public void MarkFailed(string error, DateTimeOffset utcNow)
    {
        EnsureCanTransitionFrom(WebhookProcessingStatus.Received);
        WebhookProcessingStatus = WebhookProcessingStatus.Failed;
        StatusChangeReason = null;
        ProcessingError = NormalizeRequired(error);
        ProcessedAtUtc = utcNow;
    }

    private static string NormalizeRequired(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void EnsureCanTransitionFrom(params WebhookProcessingStatus[] allowedStatuses)
    {
        if (allowedStatuses.Contains(WebhookProcessingStatus))
        {
            return;
        }

        throw new AggregateStateException(
            $"Email delivery webhook '{WebhookEventId}' cannot transition from '{WebhookProcessingStatus}'.");
    }
}
