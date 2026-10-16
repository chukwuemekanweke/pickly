using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

public sealed record SearchLocationsQuery(
    string Query,
    string? CountryCode,
    int Limit,
    ActorContext ActorContext);
