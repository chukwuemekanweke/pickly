using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDeliveryQuote;

public sealed record CreateDeliveryQuoteCommand(Guid PickupLocationId, Guid DropOffLocationId, DeliveryType DeliveryType, DeliveryOrderType OrderType, DateTimeOffset? ScheduledPickupAtUtc, Guid VehicleCategoryId, IReadOnlyCollection<DeliveryQuoteItemInput> Items, ActorContext ActorContext);
