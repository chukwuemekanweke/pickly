using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_CreatingDelivery_Should
{
    public static TheoryData<DeliveryType, DeliveryOrderType, DeliveryStatus> ValidCombinations => new()
    {
        { DeliveryType.Instant, DeliveryOrderType.PayBeforeDelivery, DeliveryStatus.AwaitingPayment },
        { DeliveryType.Instant, DeliveryOrderType.PayOnDelivery, DeliveryStatus.PendingAssignment },
        { DeliveryType.Scheduled, DeliveryOrderType.PayBeforeDelivery, DeliveryStatus.AwaitingPayment },
        { DeliveryType.Scheduled, DeliveryOrderType.PayOnDelivery, DeliveryStatus.AwaitingDispatch }
    };

    [Theory]
    [MemberData(nameof(ValidCombinations))]
    public void SetExpectedInitialState(
        DeliveryType deliveryType,
        DeliveryOrderType orderType,
        DeliveryStatus expectedStatus)
    {
        var delivery = DeliveryTestFactory.Create(deliveryType, orderType);

        delivery.Status.ShouldBe(expectedStatus);
        delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Pending);
        delivery.StatusHistory.Count.ShouldBe(1);
        delivery.StatusHistory.Single().PreviousStatus.ShouldBeNull();
        delivery.StatusHistory.Single().NewStatus.ShouldBe(expectedStatus);
        delivery.DeliveryQuoteId.ShouldNotBe(Guid.Empty);
        delivery.VehicleCategoryId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void SupportMultipleParcels()
    {
        var delivery = DeliveryTestFactory.Create(items:
        [
            DeliveryItem.Create("Documents", 1, 0.5m),
            DeliveryItem.Create("Shoes", 2, 1.75m)
        ]);

        delivery.Items.Count.ShouldBe(2);
        delivery.Items.ShouldAllBe(item => item.DeliveryId == delivery.Id);
    }

    [Fact]
    public void RejectMissingParcels() =>
        Should.Throw<ArgumentException>(() => DeliveryTestFactory.Create(items: []));

    [Fact]
    public void RejectBlankParcelDescription() =>
        Should.Throw<ArgumentException>(() => DeliveryItem.Create(" ", 1, 1m));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RejectInvalidParcelQuantity(int quantity) =>
        Should.Throw<ArgumentOutOfRangeException>(() => DeliveryItem.Create("Parcel", quantity, 1m));

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    public void RejectInvalidParcelWeight(decimal weight) =>
        Should.Throw<ArgumentOutOfRangeException>(() => DeliveryItem.Create("Parcel", 1, weight));

    [Fact]
    public void RejectMissingSender() =>
        Should.Throw<ArgumentException>(() => DeliveryTestFactory.Create(senderId: Guid.Empty));

    [Theory]
    [InlineData("", "+2348012345678")]
    [InlineData("Ada", "08012345678")]
    public void RejectInvalidRecipient(string name, string phone) =>
        Should.Throw<ArgumentException>(() => DeliveryRecipient.Create(name, phone));

    [Theory]
    [InlineData("", 6.5, 3.3)]
    [InlineData("Address", 91, 3.3)]
    [InlineData("Address", 6.5, 181)]
    public void RejectInvalidAddress(string line, decimal latitude, decimal longitude) =>
        Should.Throw<ArgumentException>(() =>
            DeliveryAddress.Create(line, null, "Lagos", "Lagos", "NG", latitude, longitude));

    [Fact]
    public void RejectNegativeCharge() =>
        Should.Throw<ArgumentOutOfRangeException>(() => DeliveryCharge.Create(-1m, DeliveryTestFactory.CurrencyId));

    [Fact]
    public void RejectScheduledDeliveryWithoutPickupTime() =>
        Should.Throw<ArgumentException>(() => Delivery.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "PKY-2026-0002",
            DeliveryType.Scheduled,
            DeliveryOrderType.PayOnDelivery,
            DeliveryTestFactory.Address("Pickup"),
            DeliveryTestFactory.Address("Dropoff"),
            DeliveryRecipient.Create("Ada", "+2348012345678"),
            DeliveryCharge.Create(100m, DeliveryTestFactory.CurrencyId),
            [DeliveryItem.Create("Parcel", 1, 1m)],
            DeliveryTestFactory.Now));

    [Fact]
    public void RejectPastScheduledPickup() =>
        Should.Throw<ArgumentOutOfRangeException>(() => DeliveryTestFactory.Create(
            DeliveryType.Scheduled,
            DeliveryOrderType.PayOnDelivery,
            scheduledPickupAtUtc: DeliveryTestFactory.Now.AddMinutes(-1),
            dispatchEligibleAtUtc: DeliveryTestFactory.Now.AddMinutes(-2)));

    [Fact]
    public void DerivePaymentResponsibilityAndMethod()
    {
        var prepaid = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayBeforeDelivery);
        var cash = DeliveryTestFactory.Create(orderType: DeliveryOrderType.PayOnDelivery, trackingId: "PKY-2026-0003");

        prepaid.PaymentResponsibleParty.ShouldBe(DeliveryPaymentResponsibleParty.Sender);
        prepaid.PaymentMethod.ShouldBe(DeliveryPaymentMethod.Digital);
        cash.PaymentResponsibleParty.ShouldBe(DeliveryPaymentResponsibleParty.Recipient);
        cash.PaymentMethod.ShouldBe(DeliveryPaymentMethod.Cash);
    }
}
