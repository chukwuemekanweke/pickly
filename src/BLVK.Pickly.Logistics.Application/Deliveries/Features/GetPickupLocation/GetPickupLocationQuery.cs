using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;

public sealed record GetPickupLocationQuery(Guid LocationId, ActorContext ActorContext);
