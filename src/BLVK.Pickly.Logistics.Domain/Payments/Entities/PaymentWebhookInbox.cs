using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class PaymentWebhookInbox : Entity, IAggregateRoot
{
    private PaymentWebhookInbox()
    {
    }

    private PaymentWebhookInbox(
        Guid paymentProviderId,
        string? merchantReference,
        string? providerReference,
        string webhookEventName,
        string? webhookEventId,
        string rawPayload,
        SignatureValidationStatus signatureValidationStatus,
        string? statusChangeReason,
        DateTimeOffset receivedAtUtc)
    {
        PaymentProviderId = paymentProviderId;
        MerchantReference = merchantReference;
        ProviderReference = providerReference;
        WebhookEventName = webhookEventName;
        WebhookEventId = webhookEventId;
        RawPayload = rawPayload;
        SignatureValidationStatus = signatureValidationStatus;
        WebhookProcessingStatus = WebhookProcessingStatus.Received;
        StatusChangeReason = statusChangeReason;
        ReceivedAtUtc = receivedAtUtc;
    }

    public Guid PaymentProviderId { get; private set; }
    public string? MerchantReference { get; private set; }
    public string? ProviderReference { get; private set; }
    public string WebhookEventName { get; private set; } = string.Empty;
    public string? WebhookEventId { get; private set; }
    public string RawPayload { get; private set; } = string.Empty;
    public WebhookProcessingStatus WebhookProcessingStatus { get; private set; }
    public SignatureValidationStatus SignatureValidationStatus { get; private set; }
    public string? StatusChangeReason { get; private set; }
    public string? ProcessingError { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public static PaymentWebhookInbox Create(
        Guid paymentProviderId,
        string? merchantReference,
        string? providerReference,
        string webhookEventName,
        string? webhookEventId,
        string rawPayload,
        SignatureValidationStatus signatureValidationStatus,
        string? statusChangeReason,
        DateTimeOffset receivedAtUtc) =>
        new(
            paymentProviderId,
            NormalizeOptional(merchantReference),
            NormalizeOptional(providerReference),
            webhookEventName.Trim(),
            NormalizeOptional(webhookEventId),
            rawPayload,
            signatureValidationStatus,
            statusChangeReason,
            receivedAtUtc);

    public void MarkProcessed(string? statusChangeReason, DateTimeOffset utcNow)
    {
        EnsureCanTransitionFrom(WebhookProcessingStatus.Received);
        WebhookProcessingStatus = WebhookProcessingStatus.Processed;
        StatusChangeReason = statusChangeReason;
        ProcessingError = null;
        ProcessedAtUtc = utcNow;
    }

    public void MarkIgnored(string? statusChangeReason, DateTimeOffset utcNow)
    {
        EnsureCanTransitionFrom(WebhookProcessingStatus.Received);
        WebhookProcessingStatus = WebhookProcessingStatus.Ignored;
        StatusChangeReason = statusChangeReason;
        ProcessingError = null;
        ProcessedAtUtc = utcNow;
    }

    public void MarkDuplicate(string? statusChangeReason, DateTimeOffset utcNow)
    {
        EnsureCanTransitionFrom(WebhookProcessingStatus.Received);
        WebhookProcessingStatus = WebhookProcessingStatus.Duplicate;
        StatusChangeReason = statusChangeReason;
        ProcessingError = null;
        ProcessedAtUtc = utcNow;
    }

    public void MarkFailed(string error, DateTimeOffset utcNow)
    {
        EnsureCanTransitionFrom(WebhookProcessingStatus.Received);
        WebhookProcessingStatus = WebhookProcessingStatus.Failed;
        ProcessingError = error;
        ProcessedAtUtc = utcNow;
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
            $"Webhook '{WebhookEventName}' cannot transition from '{WebhookProcessingStatus}'.");
    }
}
