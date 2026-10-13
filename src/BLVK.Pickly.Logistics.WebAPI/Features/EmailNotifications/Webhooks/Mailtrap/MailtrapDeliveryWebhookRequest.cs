using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.EmailNotifications.Webhooks.Mailtrap;

public sealed record MailtrapDeliveryWebhookRequest(
    [property: JsonPropertyName("events")] 
    MailtrapDeliveryWebhookEventRequest[] Events);
