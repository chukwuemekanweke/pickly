using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed record PreparedVehicleOption(VehicleCategory Vehicle, DeliveryPricingPolicy Policy, DeliveryPricingBreakdown Pricing, long DistanceMeters, string CurrencyCode);
