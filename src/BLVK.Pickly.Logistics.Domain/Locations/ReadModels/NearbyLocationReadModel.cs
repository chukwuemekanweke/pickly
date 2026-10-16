namespace BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

public sealed record NearbyLocationReadModel(LocationReadModel Location, double DistanceMeters);
