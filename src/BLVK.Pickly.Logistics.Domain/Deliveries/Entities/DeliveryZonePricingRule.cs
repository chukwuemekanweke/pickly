using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryZonePricingRule : Entity
{
    private DeliveryZonePricingRule() { }
    private DeliveryZonePricingRule(Guid policyId, Guid originZoneId, Guid destinationZoneId, decimal charge)
    {
        if (policyId == Guid.Empty || originZoneId == Guid.Empty || destinationZoneId == Guid.Empty) throw new ArgumentException("Policy and zones are required.");
        if (charge < 0m || decimal.Round(charge, 2) != charge) throw new ArgumentOutOfRangeException(nameof(charge));
        DeliveryPricingPolicyId = policyId;
        OriginZoneId = originZoneId;
        DestinationZoneId = destinationZoneId;
        Charge = charge;
    }
    public Guid DeliveryPricingPolicyId { get; private set; }
    public Guid OriginZoneId { get; private set; }
    public Guid DestinationZoneId { get; private set; }
    public decimal Charge { get; private set; }
    internal static DeliveryZonePricingRule Create(Guid policyId, Guid originZoneId, Guid destinationZoneId, decimal charge) => new(policyId, originZoneId, destinationZoneId, charge);
}
