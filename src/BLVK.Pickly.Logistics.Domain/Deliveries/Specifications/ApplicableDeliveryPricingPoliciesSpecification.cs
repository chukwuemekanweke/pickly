using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;

public sealed class ApplicableDeliveryPricingPoliciesSpecification : Specification<DeliveryPricingPolicy>
{
    public ApplicableDeliveryPricingPoliciesSpecification(DateTimeOffset atUtc, DeliveryType deliveryType, DeliveryOrderType orderType)
    {
        Where(x => x.IsActive && x.EffectiveFromUtc <= atUtc && (x.EffectiveToUtc == null || x.EffectiveToUtc > atUtc) && (x.DeliveryType == null || x.DeliveryType == deliveryType) && (x.OrderType == null || x.OrderType == orderType));
        AddInclude(x => x.VehicleRules);
        AddInclude(x => x.ZoneRules);
    }
}
