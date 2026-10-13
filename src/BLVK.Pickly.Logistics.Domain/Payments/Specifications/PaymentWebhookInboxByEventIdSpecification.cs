using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class PaymentWebhookInboxByEventIdSpecification : Specification<PaymentWebhookInbox>
{
    public PaymentWebhookInboxByEventIdSpecification(Guid paymentProviderId, string webhookEventId)
    {
        Where(inbox =>
            inbox.PaymentProviderId == paymentProviderId &&
            inbox.WebhookEventId == webhookEventId);
    }
}
