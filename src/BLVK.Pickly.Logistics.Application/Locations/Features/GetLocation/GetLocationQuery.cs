using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;

public sealed record GetLocationQuery(Guid LocationId, ActorContext ActorContext);
