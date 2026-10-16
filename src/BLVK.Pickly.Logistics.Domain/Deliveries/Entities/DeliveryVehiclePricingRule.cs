using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed class DeliveryVehiclePricingRule : Entity
{
    private DeliveryVehiclePricingRule() { }
    private DeliveryVehiclePricingRule(Guid policyId, Guid vehicleCategoryId, decimal charge)
    {
        if (policyId == Guid.Empty) throw new ArgumentException("Policy is required.", nameof(policyId));
        if (vehicleCategoryId == Guid.Empty) throw new ArgumentException("Vehicle category is required.", nameof(vehicleCategoryId));
        if (charge < 0m || decimal.Round(charge, 2) != charge) throw new ArgumentOutOfRangeException(nameof(charge));
        DeliveryPricingPolicyId = policyId;
        VehicleCategoryId = vehicleCategoryId;
        Charge = charge;
    }
    public Guid DeliveryPricingPolicyId { get; private set; }
    public Guid VehicleCategoryId { get; private set; }
    public decimal Charge { get; private set; }
    internal static DeliveryVehiclePricingRule Create(Guid policyId, Guid vehicleCategoryId, decimal charge) => new(policyId, vehicleCategoryId, charge);
}
