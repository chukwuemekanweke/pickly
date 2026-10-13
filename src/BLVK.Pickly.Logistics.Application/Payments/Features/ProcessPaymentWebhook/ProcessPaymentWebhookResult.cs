namespace BLVK.Pickly.Logistics.Application.Payments.Features.ProcessPaymentWebhook;

public sealed record ProcessPaymentWebhookResult(WebhookReceiptStatus Status);

public enum WebhookReceiptStatus
{
    Persisted = 1,
    InvalidSignature = 2,
    UnidentifiedTransaction = 3,
    Duplicate = 4
}
