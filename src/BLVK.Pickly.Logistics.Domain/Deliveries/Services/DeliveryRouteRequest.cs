using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.Services;

public sealed record DeliveryRouteRequest(Guid OriginLocationId, GeographicCoordinates Origin, Guid DestinationLocationId, GeographicCoordinates Destination, string RoutingProfile);
