using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryPayments;

public sealed record GetDeliveryPaymentsQuery(Guid DeliveryId, ActorContext ActorContext);
