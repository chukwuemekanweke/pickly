using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;

public sealed record CreatePickupLocationCommand(
    Guid LocationId,
    string Label,
    string? AdditionalDirections,
    bool IsDefault,
    ActorContext ActorContext);
