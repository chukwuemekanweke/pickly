using BLVK.Pickly.Logistics.Domain.Deliveries.Services;

namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal interface IRoutingProviderClient : IDeliveryRouteProvider
{
    string ProviderName { get; }
    bool SupportsProfile(string profile);
}
