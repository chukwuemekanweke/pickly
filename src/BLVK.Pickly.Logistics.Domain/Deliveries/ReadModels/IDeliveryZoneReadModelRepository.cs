using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public interface IDeliveryZoneReadModelRepository
{
    Task<IReadOnlyList<DeliveryZoneReadModel>> FindCoveringAsync(GeographicCoordinates coordinates, DateTimeOffset effectiveAtUtc, CancellationToken cancellationToken);
}
