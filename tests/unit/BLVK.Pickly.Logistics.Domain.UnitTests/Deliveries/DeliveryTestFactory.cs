using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

internal static class DeliveryTestFactory
{
    internal static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
    internal static readonly Guid CurrencyId = Guid.Parse("0199e63b-53c0-7000-8000-000000000001");

    internal static Delivery Create(
        DeliveryType deliveryType = DeliveryType.Instant,
        DeliveryOrderType orderType = DeliveryOrderType.PayBeforeDelivery,
        IEnumerable<DeliveryItem>? items = null,
        Guid? tenantId = null,
        Guid? senderId = null,
        DateTimeOffset? scheduledPickupAtUtc = null,
        DateTimeOffset? dispatchEligibleAtUtc = null,
        decimal amount = 2500m,
        string trackingId = "PKY-2026-0001") =>
        Delivery.Create(
            tenantId ?? Guid.CreateVersion7(),
            senderId ?? Guid.CreateVersion7(),
            trackingId,
            deliveryType,
            orderType,
            Address("1 Pickup Road"),
            Address("2 Dropoff Road"),
            DeliveryRecipient.Create("Ada Recipient", "+2348012345678"),
            DeliveryCharge.Create(amount, CurrencyId),
            items ?? [DeliveryItem.Create("Documents", 1, 0.5m)],
            Now,
            scheduledPickupAtUtc ?? (deliveryType == DeliveryType.Scheduled ? Now.AddHours(3) : null),
            dispatchEligibleAtUtc ?? (deliveryType == DeliveryType.Scheduled ? Now.AddHours(2) : null));

    internal static DeliveryAddress Address(string line) =>
        DeliveryAddress.Create(line, "Near the gate", "Lagos", "Lagos", "NG", 6.5244m, 3.3792m);

    internal static void Pay(Delivery delivery)
    {
        delivery.ConfirmVerifiedPrepaidPayment(
            Guid.CreateVersion7(),
            delivery.Charge.Amount,
            delivery.Charge.CurrencyId,
            Now.AddMinutes(1),
            "payment-worker",
            "verified_payment");
    }

    internal static void ReachInTransit(Delivery delivery)
    {
        if (delivery.Status == DeliveryStatus.AwaitingPayment)
        {
            Pay(delivery);
        }

        if (delivery.Status == DeliveryStatus.AwaitingDispatch)
        {
            delivery.OpenDispatch(delivery.DispatchEligibleAtUtc!.Value, "scheduler", "scheduled_dispatch");
        }

        delivery.AssignRider(Guid.CreateVersion7(), Now.AddHours(4), "matching", "rider_matching");
        delivery.MarkPickedUp(Now.AddHours(5), "rider", "fulfillment");
        delivery.MarkInTransit(Now.AddHours(5).AddMinutes(5), "rider", "fulfillment");
    }
}
