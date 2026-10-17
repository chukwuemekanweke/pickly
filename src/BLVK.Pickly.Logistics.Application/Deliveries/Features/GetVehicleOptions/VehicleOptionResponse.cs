namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetVehicleOptions;

public sealed record VehicleOptionResponse(Guid Id, string Code, string Name, bool IsRecommended, VehiclePriceEstimateResponse EstimatedPrice);
