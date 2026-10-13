using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;

public sealed class ProcessSafeHavenVirtualAccountTransferWebhookHandler(
    IRepository<PaymentProvider> paymentProviderRepository,
    IRepository<PaymentWebhookInbox> paymentWebhookInboxRepository,
    IRepository<PaymentTransaction> paymentTransactionRepository,
    Domain.Common.Observability.ICustomTelemetryContext customTelemetryContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ProcessSafeHavenWebhookHandlerBase<SafeHavenVirtualAccountTransferWebhookData>(
        paymentProviderRepository,
        paymentWebhookInboxRepository,
        paymentTransactionRepository,
        customTelemetryContext,
        unitOfWork,
        timeProvider)
{
    protected override SafeHavenWebhookDetails CreateWebhookDetails(SafeHavenWebhook<SafeHavenVirtualAccountTransferWebhookData> webhook)
    {
        var eventName = GetRequiredEventName(webhook.Event);

        return new SafeHavenWebhookDetails(
            NormalizeOptional(webhook.Data.PaymentReference),
            NormalizeOptional(webhook.Data.Id),
            eventName,
            CreateWebhookEventId(webhook.Data.PaymentReference, eventName),
            true);
    }
}
