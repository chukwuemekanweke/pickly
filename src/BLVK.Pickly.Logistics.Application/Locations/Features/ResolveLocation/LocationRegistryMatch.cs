using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed record LocationRegistryMatch(Location Location, string MatchKind);
