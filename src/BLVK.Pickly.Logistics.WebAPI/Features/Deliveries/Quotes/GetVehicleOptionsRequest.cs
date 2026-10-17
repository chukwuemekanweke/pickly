namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

public sealed record GetVehicleOptionsRequest(Guid PickupLocationId, Guid DropOffLocationId, string DeliveryType, string OrderType, IReadOnlyCollection<DeliveryQuoteItemRequest> Items, DateTimeOffset? ScheduledPickupAtUtc = null);
