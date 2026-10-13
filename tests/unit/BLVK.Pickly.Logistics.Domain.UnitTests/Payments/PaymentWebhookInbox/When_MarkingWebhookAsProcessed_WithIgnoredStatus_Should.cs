using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Payments.PaymentWebhookInboxes;

public sealed class When_MarkingWebhookAsProcessed_WithIgnoredStatus_Should
{
    [Fact]
    public void ThrowAggregateStateException()
    {
        var webhook = PaymentWebhookInbox.Create(
            Guid.CreateVersion7(),
            "merchant-ref",
            "provider-ref",
            "transaction.successful",
            "event-id",
            "{}",
            SignatureValidationStatus.Valid,
            null,
            DateTimeOffset.UtcNow);

        webhook.MarkIgnored("ignored", DateTimeOffset.UtcNow);

        Should.Throw<AggregateStateException>(() => webhook.MarkProcessed("processed", DateTimeOffset.UtcNow));
    }
}
