namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

public sealed record CreateDeliveryQuoteRequest(Guid PickupLocationId, Guid DropOffLocationId, string DeliveryType, string OrderType, Guid VehicleCategoryId, IReadOnlyCollection<DeliveryQuoteItemRequest> Items, DateTimeOffset? ScheduledPickupAtUtc = null);
