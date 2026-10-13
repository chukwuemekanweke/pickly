namespace BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record ProcessMailtrapDeliveryWebhookCommand(
    IReadOnlyCollection<MailtrapDeliveryWebhookEvent> Events,
    string RawPayload,
    string? SignatureHeader);
