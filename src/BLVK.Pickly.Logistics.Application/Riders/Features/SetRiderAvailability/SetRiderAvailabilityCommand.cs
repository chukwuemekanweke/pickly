using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;

public sealed record SetRiderAvailabilityCommand(bool IsAvailable, ActorContext ActorContext);
