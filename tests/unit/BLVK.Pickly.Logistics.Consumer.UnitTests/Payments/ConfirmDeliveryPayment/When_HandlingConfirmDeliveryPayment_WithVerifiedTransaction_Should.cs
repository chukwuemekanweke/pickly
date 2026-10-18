using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.ConfirmDeliveryPayment;

public sealed class When_HandlingConfirmDeliveryPayment_WithVerifiedTransaction_Should
{
    [Fact]
    public async Task ActivateTheAssociatedDeliveryExactlyOnce()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var senderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        var delivery = CreateDelivery(senderId, currencyId);
        var payment = PaymentTransaction.Create(
            "delivery-payment-ref",
            PaymentIntent.DeliveryPayment,
            Guid.CreateVersion7(),
            2500m,
            currencyId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            senderId,
            tenantId);
        payment.MarkInitiated("provider-ref", [], null, "initiated");
        payment.ConfirmVerifiedSettlement("provider-ref", 2500m, currencyId, context.Clock.GetUtcNow(), "verified");
        var attempt = DeliveryPaymentAttempt.Create(delivery.Id, payment.Id, "attempt-key");
        context.CurrentActorAccessor.ActorId.Returns(senderId.ToString());
        context.DeliveryPaymentAttemptRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<DeliveryPaymentAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempt);
        context.PaymentTransactionRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        context.DeliveryRepository.GetByIdAsync(delivery.Id, Arg.Any<CancellationToken>()).Returns(delivery);

        await context.CreateConfirmDeliveryPaymentHandler().HandleAsync(
            new ConfirmDeliveryPaymentCommand(payment.Id, payment.MerchantReference, 2500m, currencyId)
            {
                StakeholderId = senderId,
                TenantId = tenantId
            },
            CancellationToken.None);

        delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Paid);
        delivery.Status.ShouldBe(DeliveryStatus.PendingAssignment);
        delivery.PaymentTransactionId.ShouldBe(payment.Id);
        attempt.AppliedAtUtc.ShouldBe(context.Clock.GetUtcNow());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Delivery CreateDelivery(Guid senderId, Guid currencyId) =>
        Delivery.Create(
            senderId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PKL-PAYMENT-TEST",
            DeliveryType.Instant,
            DeliveryOrderType.PayBeforeDelivery,
            DeliveryAddress.Create("1 Pickup", null, "Lagos", "Lagos", "NG", 6.5m, 3.3m),
            DeliveryAddress.Create("2 Dropoff", null, "Lagos", "Lagos", "NG", 6.6m, 3.4m),
            DeliveryRecipient.Create("Recipient", "+2348012345678"),
            DeliveryCharge.Create(2500m, currencyId),
            [DeliveryItem.Create("Parcel", 1, 1m)],
            new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero));
}
