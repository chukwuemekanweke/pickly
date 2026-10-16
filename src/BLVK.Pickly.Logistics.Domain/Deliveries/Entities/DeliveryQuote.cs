using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryQuote : Entity, IAggregateRoot
{
    private readonly List<DeliveryQuoteItem> _items = [];
    private DeliveryQuote() { }

    private DeliveryQuote(Guid senderId, Guid pickupLocationId, Guid dropOffLocationId, DeliveryAddress pickupAddress, DeliveryAddress dropOffAddress, DeliveryType deliveryType, DeliveryOrderType orderType, DateTimeOffset? scheduledPickupAtUtc, VehicleCategory vehicle, DeliveryZoneReadModels zones, DeliveryPricingPolicy policy, DeliveryPricingBreakdown pricing, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        SenderId = senderId;
        PickupLocationId = pickupLocationId;
        DropOffLocationId = dropOffLocationId;
        PickupAddress = pickupAddress;
        DropOffAddress = dropOffAddress;
        DeliveryType = deliveryType;
        OrderType = orderType;
        ScheduledPickupAtUtc = scheduledPickupAtUtc?.ToUniversalTime();
        VehicleCategoryId = vehicle.Id;
        VehicleCategoryCode = vehicle.Code;
        VehicleCategoryName = vehicle.Name;
        OriginZoneId = zones.OriginId;
        OriginZoneCode = zones.OriginCode;
        DestinationZoneId = zones.DestinationId;
        DestinationZoneCode = zones.DestinationCode;
        PricingPolicyId = policy.Id;
        PricingPolicyVersion = policy.Version;
        CurrencyId = policy.CurrencyId;
        Pricing = pricing;
        TotalAmount = pricing.TotalAmount;
        IssuedAtUtc = createdAtUtc.ToUniversalTime();
        ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        ConsumptionStatus = DeliveryQuoteConsumptionStatus.Active;
    }

    public Guid SenderId { get; private set; }
    public Guid PickupLocationId { get; private set; }
    public Guid DropOffLocationId { get; private set; }
    public DeliveryAddress PickupAddress { get; private set; } = null!;
    public DeliveryAddress DropOffAddress { get; private set; } = null!;
    public DeliveryType DeliveryType { get; private set; }
    public DeliveryOrderType OrderType { get; private set; }
    public DateTimeOffset? ScheduledPickupAtUtc { get; private set; }
    public Guid VehicleCategoryId { get; private set; }
    public string VehicleCategoryCode { get; private set; } = string.Empty;
    public string VehicleCategoryName { get; private set; } = string.Empty;
    public decimal TotalWeightKg { get; private set; }
    public Guid OriginZoneId { get; private set; }
    public string OriginZoneCode { get; private set; } = string.Empty;
    public Guid DestinationZoneId { get; private set; }
    public string DestinationZoneCode { get; private set; } = string.Empty;
    public Guid PricingPolicyId { get; private set; }
    public string PricingPolicyVersion { get; private set; } = string.Empty;
    public DeliveryPricingBreakdown Pricing { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public Guid CurrencyId { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DeliveryQuoteConsumptionStatus ConsumptionStatus { get; private set; }
    public uint RowVersion { get; private set; }
    public Guid? ConsumedByDeliveryId { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public IReadOnlyCollection<DeliveryQuoteItem> Items => _items;

    public DeliveryPaymentResponsibleParty PaymentResponsibleParty => OrderType == DeliveryOrderType.PayBeforeDelivery ? DeliveryPaymentResponsibleParty.Sender : DeliveryPaymentResponsibleParty.Recipient;
    public DeliveryPaymentMethod PaymentMethod => OrderType == DeliveryOrderType.PayBeforeDelivery ? DeliveryPaymentMethod.Digital : DeliveryPaymentMethod.Cash;
    public bool IsExpired(DateTimeOffset utcNow) => utcNow.ToUniversalTime() >= ExpiresAtUtc;

    public static DeliveryQuote Create(Guid senderId, Guid pickupLocationId, Guid dropOffLocationId, DeliveryAddress pickupAddress, DeliveryAddress dropOffAddress, DeliveryType deliveryType, DeliveryOrderType orderType, DateTimeOffset? scheduledPickupAtUtc, VehicleCategory vehicle, DeliveryZoneReadModels zones, DeliveryPricingPolicy policy, DeliveryPricingBreakdown pricing, IEnumerable<(string Description, int Quantity, decimal WeightKg)> items, DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
    {
        if (senderId == Guid.Empty || pickupLocationId == Guid.Empty || dropOffLocationId == Guid.Empty) throw new ArgumentException("Sender and locations are required.");
        if (!Enum.IsDefined(deliveryType) || !Enum.IsDefined(orderType)) throw new ArgumentOutOfRangeException(nameof(deliveryType));
        if (deliveryType == DeliveryType.Scheduled && (scheduledPickupAtUtc is null || scheduledPickupAtUtc <= createdAtUtc)) throw new ArgumentException("A future pickup time is required for scheduled delivery.", nameof(scheduledPickupAtUtc));
        if (deliveryType == DeliveryType.Instant && scheduledPickupAtUtc is not null) throw new ArgumentException("Instant delivery cannot have a scheduled pickup time.", nameof(scheduledPickupAtUtc));
        if (expiresAtUtc <= createdAtUtc) throw new ArgumentOutOfRangeException(nameof(expiresAtUtc));
        var snapshots = items?.ToArray() ?? throw new ArgumentNullException(nameof(items));
        var totalWeight = DeliveryPricingCalculator.CalculateTotalWeight(snapshots.Select(x => (x.Quantity, x.WeightKg)));
        if (!vehicle.CanCarry(totalWeight)) throw new ArgumentException("Vehicle cannot carry the shipment.", nameof(vehicle));
        var quote = new DeliveryQuote(senderId, pickupLocationId, dropOffLocationId, pickupAddress, dropOffAddress, deliveryType, orderType, scheduledPickupAtUtc, vehicle, zones, policy, pricing, createdAtUtc, expiresAtUtc) { TotalWeightKg = totalWeight };
        quote._items.AddRange(snapshots.Select(x => DeliveryQuoteItem.Create(quote.Id, x.Description, x.Quantity, x.WeightKg)));
        return quote;
    }

    public void Consume(Guid deliveryId, DateTimeOffset utcNow)
    {
        if (deliveryId == Guid.Empty) throw new ArgumentException("Delivery is required.", nameof(deliveryId));
        if (ConsumptionStatus == DeliveryQuoteConsumptionStatus.Consumed)
        {
            if (ConsumedByDeliveryId == deliveryId) return;
            throw new AggregateStateException("Quote has already been consumed.");
        }
        if (IsExpired(utcNow)) throw new AggregateStateException("Quote has expired.");
        ConsumptionStatus = DeliveryQuoteConsumptionStatus.Consumed;
        ConsumedByDeliveryId = deliveryId;
        ConsumedAtUtc = utcNow.ToUniversalTime();
    }
}
