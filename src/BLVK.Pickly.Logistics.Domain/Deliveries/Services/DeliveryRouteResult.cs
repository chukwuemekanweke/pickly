namespace BLVK.Pickly.Logistics.Domain.Deliveries.Services;

public sealed record DeliveryRouteResult(DeliveryRouteStatus Status, long DistanceMeters = 0, TimeSpan? Duration = null, string Provider = "", string RoutingProfile = "", TimeSpan? RetryAfter = null);
