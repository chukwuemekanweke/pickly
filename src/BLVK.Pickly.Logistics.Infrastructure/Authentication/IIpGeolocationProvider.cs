using BLVK.Pickly.Logistics.Domain.Authentication.Services;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal interface IIpGeolocationProvider
{
    Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken);
}
