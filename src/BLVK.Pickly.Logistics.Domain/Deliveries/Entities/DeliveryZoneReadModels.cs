namespace BLVK.Pickly.Logistics.Domain.Deliveries.Entities;

public sealed record DeliveryZoneReadModels(Guid OriginId, string OriginCode, Guid DestinationId, string DestinationCode);
