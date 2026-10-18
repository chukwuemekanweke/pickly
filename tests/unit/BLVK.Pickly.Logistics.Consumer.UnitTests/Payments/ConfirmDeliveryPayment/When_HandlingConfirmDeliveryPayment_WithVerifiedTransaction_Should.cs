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

        var command = new ConfirmDeliveryPaymentCommand(payment.Id, payment.MerchantReference, 2500m, currencyId)
        {
            StakeholderId = senderId,
            TenantId = tenantId
        };
        var handler = context.CreateConfirmDeliveryPaymentHandler();
        await handler.HandleAsync(command, CancellationToken.None);
        var statusHistoryCount = delivery.StatusHistory.Count;
        await handler.HandleAsync(command, CancellationToken.None);

        delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Paid);
        delivery.Status.ShouldBe(DeliveryStatus.PendingAssignment);
        delivery.PaymentTransactionId.ShouldBe(payment.Id);
        delivery.StatusHistory.Count.ShouldBe(statusHistoryCount);
        attempt.AppliedAtUtc.ShouldBe(context.Clock.GetUtcNow());
        await context.UnitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateScheduledDeliveryToAwaitingDispatch()
    {
        var setup = CreateSetup(DeliveryType.Scheduled);

        await setup.Context.CreateConfirmDeliveryPaymentHandler().HandleAsync(
            setup.Command,
            CancellationToken.None);

        setup.Delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Paid);
        setup.Delivery.Status.ShouldBe(DeliveryStatus.AwaitingDispatch);
        setup.Attempt.AppliedAtUtc.ShouldBe(setup.Context.Clock.GetUtcNow());
    }

    [Fact]
    public async Task PreserveLatePaymentAfterDeliveryCancellationForInvestigation()
    {
        var setup = CreateSetup();
        setup.Delivery.Cancel("sender_cancelled", setup.Context.Clock.GetUtcNow(), "sender", "api");

        await setup.Context.CreateConfirmDeliveryPaymentHandler().HandleAsync(
            setup.Command,
            CancellationToken.None);

        setup.Delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Pending);
        setup.Delivery.Status.ShouldBe(DeliveryStatus.Cancelled);
        setup.Attempt.AppliedAtUtc.ShouldBeNull();
        setup.Attempt.ApplicationException!.ShouldContain("after delivery cancellation");
    }

    [Fact]
    public async Task PreserveAdditionalSuccessfulPaymentForInvestigation()
    {
        var setup = CreateSetup();
        var satisfyingTransactionId = Guid.CreateVersion7();
        setup.Delivery.ConfirmVerifiedPrepaidPayment(
            satisfyingTransactionId,
            setup.Delivery.Charge.Amount,
            setup.Delivery.Charge.CurrencyId,
            setup.Context.Clock.GetUtcNow(),
            "worker",
            "verified");

        await setup.Context.CreateConfirmDeliveryPaymentHandler().HandleAsync(
            setup.Command,
            CancellationToken.None);

        setup.Delivery.PaymentTransactionId.ShouldBe(satisfyingTransactionId);
        setup.Attempt.AppliedAtUtc.ShouldBeNull();
        setup.Attempt.ApplicationException!.ShouldContain("Another successful transaction");
    }

    private static PaymentConfirmationSetup CreateSetup(DeliveryType deliveryType = DeliveryType.Instant)
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var senderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();
        var delivery = CreateDelivery(senderId, currencyId, deliveryType);
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
        payment.ConfirmVerifiedSettlement(
            "provider-ref", 2500m, currencyId, context.Clock.GetUtcNow(), "verified", "NGN");
        var attempt = DeliveryPaymentAttempt.Create(delivery.Id, payment.Id, "attempt-key");
        context.CurrentActorAccessor.ActorId.Returns(senderId.ToString());
        context.DeliveryPaymentAttemptRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<DeliveryPaymentAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempt);
        context.PaymentTransactionRepository.GetByIdAsync(payment.Id, Arg.Any<CancellationToken>()).Returns(payment);
        context.DeliveryRepository.GetByIdAsync(delivery.Id, Arg.Any<CancellationToken>()).Returns(delivery);
        var command = new ConfirmDeliveryPaymentCommand(payment.Id, payment.MerchantReference, 2500m, currencyId)
        {
            StakeholderId = senderId,
            TenantId = tenantId
        };
        return new(context, delivery, attempt, command);
    }

    private static Delivery CreateDelivery(
        Guid senderId,
        Guid currencyId,
        DeliveryType deliveryType = DeliveryType.Instant) =>
        Delivery.Create(
            senderId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PKL-PAYMENT-TEST",
            deliveryType,
            DeliveryOrderType.PayBeforeDelivery,
            DeliveryAddress.Create("1 Pickup", null, "Lagos", "Lagos", "NG", 6.5m, 3.3m),
            DeliveryAddress.Create("2 Dropoff", null, "Lagos", "Lagos", "NG", 6.6m, 3.4m),
            DeliveryRecipient.Create("Recipient", "+2348012345678"),
            DeliveryCharge.Create(2500m, currencyId),
            [DeliveryItem.Create("Parcel", 1, 1m)],
            new DateTimeOffset(2026, 10, 18, 10, 0, 0, TimeSpan.Zero),
            deliveryType == DeliveryType.Scheduled
                ? new DateTimeOffset(2026, 10, 18, 13, 0, 0, TimeSpan.Zero)
                : null,
            deliveryType == DeliveryType.Scheduled
                ? new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.Zero)
                : null);

    private sealed record PaymentConfirmationSetup(
        PaymentsConsumerTestContext Context,
        Delivery Delivery,
        DeliveryPaymentAttempt Attempt,
        ConfirmDeliveryPaymentCommand Command);
}
