using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_CreatingDeliveryPaymentAttempt_Should
{
    [Fact]
    public void KeepOnlyTheDeliveryOwnedAssociationMetadata()
    {
        var deliveryId = Guid.CreateVersion7();
        var paymentTransactionId = Guid.CreateVersion7();

        var attempt = DeliveryPaymentAttempt.Create(deliveryId, paymentTransactionId, " retry-key ");

        attempt.DeliveryId.ShouldBe(deliveryId);
        attempt.PaymentTransactionId.ShouldBe(paymentTransactionId);
        attempt.IdempotencyKey.ShouldBe("retry-key");
        typeof(Domain.Payments.Entities.PaymentTransaction).GetProperty("DeliveryId").ShouldBeNull();
    }

    [Fact]
    public void RecordOperationalApplicationOutcomeWithoutDuplicatingPaymentStatus()
    {
        var attempt = DeliveryPaymentAttempt.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null);
        var appliedAt = DeliveryTestFactory.Now;

        attempt.RecordApplicationException("late payment");
        attempt.RecordApplied(appliedAt);

        attempt.AppliedAtUtc.ShouldBe(appliedAt);
        attempt.ApplicationException.ShouldBeNull();
    }
}
