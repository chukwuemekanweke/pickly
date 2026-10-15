using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;

public sealed record CreatePickupLocationCommand(
    string Label,
    PickupLocationAddress Address,
    bool IsDefault,
    ActorContext ActorContext);
