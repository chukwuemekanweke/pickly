using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed record DeliveryQuotePreparation(DeliveryQuotePreparationStatus Status, Guid PickupLocationId = default, Guid DropOffLocationId = default, DeliveryAddress? PickupAddress = null, DeliveryAddress? DropOffAddress = null, DeliveryZoneReadModels? Zones = null, decimal TotalWeightKg = 0m, IReadOnlyList<PreparedVehicleOption>? Options = null);
