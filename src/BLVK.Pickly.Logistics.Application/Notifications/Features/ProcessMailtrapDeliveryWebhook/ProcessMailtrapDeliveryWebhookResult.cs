namespace BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookResult(
    MailtrapDeliveryWebhookReceiptStatus Status,
    string? StatusChangeReason = null);

public enum MailtrapDeliveryWebhookReceiptStatus
{
    Persisted = 1,
    Duplicate = 2,
    InvalidSignature = 3
}
