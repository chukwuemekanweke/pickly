using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Deliveries;

public sealed class When_ConsumingDeliveryQuote_Should
{
    [Fact]
    public void AllowIdempotentSameDelivery_AndRejectAnotherDelivery()
    {
        var now = new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero);
        var quote = CreateQuote(now, now.AddMinutes(15));
        var deliveryId = Guid.CreateVersion7();
        quote.Consume(deliveryId, now.AddMinutes(1));
        quote.Consume(deliveryId, now.AddMinutes(2));
        quote.ConsumptionStatus.ShouldBe(DeliveryQuoteConsumptionStatus.Consumed);
        quote.ConsumedByDeliveryId.ShouldBe(deliveryId);
        Should.Throw<AggregateStateException>(() => quote.Consume(Guid.CreateVersion7(), now.AddMinutes(3)));
    }

    [Fact]
    public void RejectExpiredQuote()
    {
        var now = new DateTimeOffset(2026, 10, 16, 10, 0, 0, TimeSpan.Zero);
        var quote = CreateQuote(now, now.AddMinutes(15));
        Should.Throw<AggregateStateException>(() => quote.Consume(Guid.CreateVersion7(), now.AddMinutes(15)));
    }

    private static DeliveryQuote CreateQuote(DateTimeOffset now, DateTimeOffset expires)
    {
        var vehicle = VehicleCategory.Create("TEST", "Test", null, 100m, "driving");
        var origin = Guid.CreateVersion7();
        var destination = Guid.CreateVersion7();
        var policy = DeliveryPricingPolicy.Create("v1", Guid.CreateVersion7(), 1m, 1m, 1m, now.AddDays(-1));
        policy.AddVehicleRule(vehicle.Id, 1m);
        policy.AddZoneRule(origin, destination, 1m);
        var price = DeliveryPricingCalculator.Calculate(policy, vehicle.Id, origin, destination, 1000, 1m);
        var address = DeliveryAddress.Create("1 Street", null, "Enugu", "Enugu", "NG", 6.4m, 7.5m);
        return DeliveryQuote.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), address, address, DeliveryType.Instant, DeliveryOrderType.PayOnDelivery, null, vehicle, new(origin, "A", destination, "B"), policy, price, [("Parcel", 1, 1m)], now, expires);
    }
}
