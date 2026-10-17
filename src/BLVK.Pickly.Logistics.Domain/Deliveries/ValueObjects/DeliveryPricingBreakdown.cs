namespace BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

public sealed record DeliveryPricingBreakdown(decimal BaseFee, decimal DistanceCharge, decimal WeightCharge, decimal VehicleCharge, decimal ZoneCharge)
{
    public decimal TotalAmount => checked(BaseFee + DistanceCharge + WeightCharge + VehicleCharge + ZoneCharge);
}
