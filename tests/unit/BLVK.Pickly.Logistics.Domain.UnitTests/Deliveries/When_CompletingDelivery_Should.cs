using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_CompletingDelivery_Should
{
    [Fact]
    public void ActivateInstantPrepaidOrderAfterVerifiedFullPayment()
    {
        var delivery = DeliveryTestFactory.Create();

        DeliveryTestFactory.Pay(delivery);

        delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Paid);
        delivery.Status.ShouldBe(DeliveryStatus.PendingAssignment);
        delivery.IsMatchingEligible.ShouldBeTrue();
    }

    [Fact]
    public void RejectIncorrectPrepaidAmountOrCurrency()
    {
        var delivery = DeliveryTestFactory.Create();

        Should.Throw<AggregateStateException>(() => delivery.ConfirmVerifiedPrepaidPayment(
            Guid.CreateVersion7(), 2499m, delivery.Charge.CurrencyId, DeliveryTestFactory.Now, "worker", "payment"));
        Should.Throw<AggregateStateException>(() => delivery.ConfirmVerifiedPrepaidPayment(
            Guid.CreateVersion7(), 2500m, Guid.CreateVersion7(), DeliveryTestFactory.Now, "worker", "payment"));
    }

    [Fact]
    public void TreatRepeatedSameVerifiedPaymentAsIdempotent()
    {
        var delivery = DeliveryTestFactory.Create();
        var transactionId = Guid.CreateVersion7();
        delivery.ConfirmVerifiedPrepaidPayment(
            transactionId, 2500m, DeliveryTestFactory.CurrencyId, DeliveryTestFactory.Now, "worker", "payment");
        var historyCount = delivery.StatusHistory.Count;

        delivery.ConfirmVerifiedPrepaidPayment(
            transactionId, 2500m, DeliveryTestFactory.CurrencyId, DeliveryTestFactory.Now, "worker", "payment");

        delivery.StatusHistory.Count.ShouldBe(historyCount);
    }

    [Fact]
    public void RequireProofBeforeCompletion()
    {
        var delivery = DeliveryTestFactory.Create();
        DeliveryTestFactory.ReachInTransit(delivery);

        Should.Throw<AggregateStateException>(() =>
            delivery.Complete(DeliveryTestFactory.Now.AddHours(6), "rider", "fulfillment"));
    }

    [Fact]
    public void RequireFullCashCollectionForCashOrder()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        DeliveryTestFactory.ReachInTransit(delivery);
        delivery.ConfirmProofOfDelivery(Guid.CreateVersion7());

        Should.Throw<AggregateStateException>(() => delivery.ConfirmFullCashCollection(
            Guid.CreateVersion7(), 1000m, DeliveryTestFactory.CurrencyId, DeliveryTestFactory.Now));
        Should.Throw<AggregateStateException>(() =>
            delivery.Complete(DeliveryTestFactory.Now.AddHours(6), "rider", "fulfillment"));
    }

    [Fact]
    public void CompleteCashOrderWithoutRequiringRemittance()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        DeliveryTestFactory.ReachInTransit(delivery);
        delivery.ConfirmFullCashCollection(
            Guid.CreateVersion7(), 2500m, DeliveryTestFactory.CurrencyId, DeliveryTestFactory.Now.AddHours(6));
        delivery.ConfirmProofOfDelivery(Guid.CreateVersion7());

        delivery.Complete(DeliveryTestFactory.Now.AddHours(6), "rider", "fulfillment");

        delivery.Status.ShouldBe(DeliveryStatus.Delivered);
    }

    [Fact]
    public void RejectCashCollectionInWrongCurrency()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        DeliveryTestFactory.ReachInTransit(delivery);

        Should.Throw<AggregateStateException>(() => delivery.ConfirmFullCashCollection(
            Guid.CreateVersion7(), 2500m, Guid.CreateVersion7(), DeliveryTestFactory.Now.AddHours(6)));
    }

    [Fact]
    public void CompletePrepaidOrderAfterProof()
    {
        var delivery = DeliveryTestFactory.Create();
        DeliveryTestFactory.ReachInTransit(delivery);
        delivery.ConfirmProofOfDelivery(Guid.CreateVersion7());

        delivery.Complete(DeliveryTestFactory.Now.AddHours(6), "rider", "fulfillment");

        delivery.Status.ShouldBe(DeliveryStatus.Delivered);
    }

    [Fact]
    public void PreventSecondCompletionFromTerminalState()
    {
        var delivery = DeliveryTestFactory.Create();
        DeliveryTestFactory.ReachInTransit(delivery);
        delivery.ConfirmProofOfDelivery(Guid.CreateVersion7());
        delivery.Complete(DeliveryTestFactory.Now.AddHours(6), "rider", "fulfillment");

        Should.Throw<AggregateStateException>(() =>
            delivery.Complete(DeliveryTestFactory.Now.AddHours(7), "rider", "fulfillment"));
    }

    [Fact]
    public void PreserveAddressSnapshotValues()
    {
        var pickup = DeliveryTestFactory.Address("Original pickup");
        var delivery = Domain.Deliveries.Entities.Delivery.Create(
            Guid.CreateVersion7(), "PKY-2026-0004",
            DeliveryType.Instant, DeliveryOrderType.PayOnDelivery,
            pickup, DeliveryTestFactory.Address("Dropoff"),
            Domain.Deliveries.ValueObjects.DeliveryRecipient.Create("Ada", "+2348012345678"),
            Domain.Deliveries.ValueObjects.DeliveryCharge.Create(2500m, DeliveryTestFactory.CurrencyId),
            [Domain.Deliveries.Entities.DeliveryItem.Create("Parcel", 1, 1m)],
            DeliveryTestFactory.Now);

        delivery.PickupAddress.AddressLine.ShouldBe("Original pickup");
    }
}
