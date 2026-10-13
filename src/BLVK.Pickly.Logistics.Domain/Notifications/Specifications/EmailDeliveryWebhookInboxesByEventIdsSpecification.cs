using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class EmailDeliveryWebhookInboxesByEventIdsSpecification : Specification<EmailDeliveryWebhookInbox>
{
    public EmailDeliveryWebhookInboxesByEventIdsSpecification(Guid providerId, IReadOnlyCollection<string> webhookEventIds)
    {
        Where(inbox => inbox.ProviderId == providerId && webhookEventIds.Contains(inbox.WebhookEventId));
    }
}
