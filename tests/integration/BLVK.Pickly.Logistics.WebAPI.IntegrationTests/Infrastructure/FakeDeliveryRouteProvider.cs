using BLVK.Pickly.Logistics.Domain.Deliveries.Services;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

public sealed class FakeDeliveryRouteProvider : IDeliveryRouteProvider
{
    public Func<DeliveryRouteRequest, DeliveryRouteResult>? Responder { get; set; }
    public int RequestCount { get; private set; }
    public Task<DeliveryRouteResult> GetRouteAsync(DeliveryRouteRequest request, CancellationToken cancellationToken)
    {
        RequestCount++;
        return Task.FromResult(Responder?.Invoke(request) ?? new DeliveryRouteResult(DeliveryRouteStatus.ProviderUnavailable));
    }
    public void Reset() { Responder = null; RequestCount = 0; }
}
