namespace BLVK.Pickly.Logistics.Domain.Notifications.Services;

public sealed record MailtrapWebhookSignatureValidationResult(bool IsValid, string StatusChangeReason);
