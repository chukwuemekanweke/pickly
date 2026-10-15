using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;

public sealed record DeletePickupLocationCommand(Guid LocationId, ActorContext ActorContext);
