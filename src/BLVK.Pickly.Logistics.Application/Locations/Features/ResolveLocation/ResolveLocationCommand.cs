using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed record ResolveLocationCommand(
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    ActorContext ActorContext);
