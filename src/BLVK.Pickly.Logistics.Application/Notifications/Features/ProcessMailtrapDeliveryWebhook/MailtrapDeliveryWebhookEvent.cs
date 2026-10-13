namespace BLVK.Pickly.Logistics.Application.Notifications.Features.ProcessMailtrapDeliveryWebhook;

public sealed record MailtrapDeliveryWebhookEvent(
    string Event,
    string MessageId,
    string SendingStream,
    string Email,
    string SendingDomainName,
    long Timestamp,
    string EventId);
