using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetVehicleOptions;

public sealed record GetVehicleOptionsQuery(Guid PickupLocationId, Guid DropOffLocationId, DeliveryType DeliveryType, DeliveryOrderType OrderType, DateTimeOffset? ScheduledPickupAtUtc, IReadOnlyCollection<DeliveryQuoteItemInput> Items, ActorContext ActorContext);
