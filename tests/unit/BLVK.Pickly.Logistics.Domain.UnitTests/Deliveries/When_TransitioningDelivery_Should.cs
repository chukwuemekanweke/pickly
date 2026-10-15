using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_TransitioningDelivery_Should
{
    [Fact]
    public void PreventPrepaidMatchingBeforePayment()
    {
        var delivery = DeliveryTestFactory.Create();

        delivery.IsMatchingEligible.ShouldBeFalse();
        Should.Throw<AggregateStateException>(() =>
            delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching"));
    }

    [Fact]
    public void AllowInstantCashOrderMatchingWithoutAdvancePayment()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);

        delivery.IsMatchingEligible.ShouldBeTrue();
        delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching");

        delivery.Status.ShouldBe(DeliveryStatus.RiderAssigned);
        delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Pending);
    }

    [Fact]
    public void KeepScheduledOrderInactiveUntilDispatchEligibility()
    {
        var delivery = DeliveryTestFactory.Create(DeliveryType.Scheduled, DeliveryOrderType.PayOnDelivery);
        var dispatchEligibleAtUtc = delivery.DispatchEligibleAtUtc.GetValueOrDefault();

        Should.Throw<AggregateStateException>(() =>
            delivery.OpenDispatch(dispatchEligibleAtUtc.AddTicks(-1), "scheduler", "scheduled_dispatch"));

        delivery.OpenDispatch(dispatchEligibleAtUtc, "scheduler", "scheduled_dispatch");
        delivery.IsMatchingEligible.ShouldBeTrue();
    }

    [Fact]
    public void KeepScheduledPrepaidOrderInactiveAfterPaymentUntilDispatchEligibility()
    {
        var delivery = DeliveryTestFactory.Create(DeliveryType.Scheduled, DeliveryOrderType.PayBeforeDelivery);
        DeliveryTestFactory.Pay(delivery);

        delivery.Status.ShouldBe(DeliveryStatus.AwaitingDispatch);
        delivery.IsMatchingEligible.ShouldBeFalse();
        Should.Throw<AggregateStateException>(() =>
            delivery.OpenDispatch(delivery.DispatchEligibleAtUtc.GetValueOrDefault().AddTicks(-1), "scheduler", "scheduled_dispatch"));
    }

    [Fact]
    public void PreventDuplicateRiderAssignment()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching");

        Should.Throw<AggregateStateException>(() =>
            delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching"));
    }

    [Fact]
    public void PreventPickupBeforeAssignment()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);

        Should.Throw<AggregateStateException>(() =>
            delivery.MarkPickedUp(DeliveryTestFactory.Now, "rider", "fulfillment"));
    }

    [Fact]
    public void PreventTransitBeforePickup()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching");

        Should.Throw<AggregateStateException>(() =>
            delivery.MarkInTransit(DeliveryTestFactory.Now, "rider", "fulfillment"));
    }

    [Fact]
    public void AppendCorrectHistoryForEachTransition()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching");

        var history = delivery.StatusHistory.Last();
        history.PreviousStatus.ShouldBe(DeliveryStatus.PendingAssignment);
        history.NewStatus.ShouldBe(DeliveryStatus.RiderAssigned);
        history.Actor.ShouldBe("matching");
        history.Source.ShouldBe("rider_matching");
    }

    [Fact]
    public void KeepTrackingAndOwnershipStableAcrossTransitions()
    {
        var senderId = Guid.CreateVersion7();
        var delivery = DeliveryTestFactory.Create(
            orderType: DeliveryOrderType.PayOnDelivery,
            senderId: senderId);

        delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching");

        delivery.TrackingId.ShouldBe("PKY-2026-0001");
        delivery.SenderId.ShouldBe(senderId);
    }

    [Fact]
    public void PreventParcelChangesAfterFulfillmentBegins()
    {
        var delivery = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        delivery.AssignRider(Guid.CreateVersion7(), DeliveryTestFactory.Now, "matching", "rider_matching");

        Should.Throw<AggregateStateException>(() =>
            delivery.AddItem(Domain.Deliveries.Entities.DeliveryItem.Create("Late parcel", 1, 1m)));
    }

    [Fact]
    public void SupportCancellationBeforePickupAndFailureDuringFulfillment()
    {
        var cancelled = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery);
        cancelled.Cancel("Sender cancelled.", DeliveryTestFactory.Now, "sender", "delivery_management");
        cancelled.Status.ShouldBe(DeliveryStatus.Cancelled);

        var failed = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery, trackingId: "PKY-2026-FAILED");
        DeliveryTestFactory.ReachInTransit(failed);
        failed.MarkFailed("Recipient unavailable.", DeliveryTestFactory.Now.AddHours(6), "rider", "fulfillment");
        failed.Status.ShouldBe(DeliveryStatus.DeliveryFailed);
    }
}
