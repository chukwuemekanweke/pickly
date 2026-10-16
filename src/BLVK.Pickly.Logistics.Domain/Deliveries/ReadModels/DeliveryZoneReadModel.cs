namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public sealed record DeliveryZoneReadModel(Guid Id, string Code, string Name, string CountryCode, int Priority);
