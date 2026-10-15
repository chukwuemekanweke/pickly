namespace BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

public interface ILocationReadModelRepository
{
    Task<LocationReadModel?> GetByIdAsync(Guid locationId, CancellationToken cancellationToken);

    /// <summary>Text search over publicly discoverable, usable locations, ranked by relevance.</summary>
    Task<IReadOnlyList<LocationReadModel>> SearchAsync(
        LocationSearchRequest request,
        CancellationToken cancellationToken);

    /// <summary>Publicly discoverable, usable locations within a radius in metres, nearest first.</summary>
    Task<IReadOnlyList<NearbyLocationReadModel>> FindNearbyAsync(
        NearbyLocationsRequest request,
        CancellationToken cancellationToken);
}
