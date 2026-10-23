using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Domain.Operations.ReadModels;

public sealed record DeliveryStatusCount(DeliveryStatus Status, int Count);
