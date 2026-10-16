using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;

public sealed record UpdatePickupLocationCommand(
    Guid PickupLocationId,
    Guid LocationId,
    string Label,
    string? AdditionalDirections,
    ActorContext ActorContext);
