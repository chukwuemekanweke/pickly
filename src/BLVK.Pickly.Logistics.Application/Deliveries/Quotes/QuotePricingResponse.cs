namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed record QuotePricingResponse(decimal BaseFee, decimal DistanceCharge, decimal WeightCharge, decimal VehicleCharge, decimal ZoneCharge, decimal TotalAmount, string CurrencyCode);
