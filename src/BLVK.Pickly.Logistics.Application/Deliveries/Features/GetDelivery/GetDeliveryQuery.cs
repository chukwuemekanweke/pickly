using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDelivery;

public sealed record GetDeliveryQuery(Guid DeliveryId, ActorContext ActorContext);
