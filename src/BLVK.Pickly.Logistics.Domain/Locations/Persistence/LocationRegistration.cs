using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Domain.Locations.Persistence;

public sealed record LocationRegistration(Location Location, bool Created);
