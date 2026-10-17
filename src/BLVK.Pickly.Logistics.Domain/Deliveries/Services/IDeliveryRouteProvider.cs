namespace BLVK.Pickly.Logistics.Domain.Deliveries.Services;

public interface IDeliveryRouteProvider
{
    Task<DeliveryRouteResult> GetRouteAsync(DeliveryRouteRequest request, CancellationToken cancellationToken);
}
