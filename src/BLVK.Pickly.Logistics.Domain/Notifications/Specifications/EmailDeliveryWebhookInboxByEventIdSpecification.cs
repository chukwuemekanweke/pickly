using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class EmailDeliveryWebhookInboxByEventIdSpecification : Specification<EmailDeliveryWebhookInbox>
{
    public EmailDeliveryWebhookInboxByEventIdSpecification(Guid providerId, string webhookEventId)
    {
        Where(inbox => inbox.ProviderId == providerId && inbox.WebhookEventId == webhookEventId);
    }
}
