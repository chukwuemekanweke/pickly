using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;

public sealed record UpdatePickupLocationCommand(
    Guid LocationId,
    string Label,
    PickupLocationAddress Address,
    ActorContext ActorContext);
