using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetVehicleOptions;

public sealed record GetVehicleOptionsResult(DeliveryQuotePreparationStatus Status, Guid? RecommendedVehicleCategoryId = null, IReadOnlyList<VehicleOptionResponse>? Options = null);
