using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;

public sealed record SetDefaultPickupLocationCommand(Guid LocationId, ActorContext ActorContext);
