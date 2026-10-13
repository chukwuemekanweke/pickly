using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using Microsoft.Extensions.Logging;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

internal sealed class IpGeolocationService(
    IEnumerable<IIpGeolocationProvider> providers,
    ILogger<IpGeolocationService> logger) : IIpGeolocationService
{
    public async Task<IpGeolocation?> GetGeolocationAsync(string ipAddress, CancellationToken cancellationToken)
    {
        if (!IpAddressUtility.IsPublicIpAddress(ipAddress))
        {
            return null;
        }

        foreach (var provider in providers)
        {
            try
            {
                var geolocation = await provider.GetGeolocationAsync(ipAddress, cancellationToken);
                if (geolocation is not null)
                {
                    return geolocation;
                }
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                IpGeolocationLog.ProviderTimedOut(logger, exception, provider.GetType().Name);
            }
            catch (HttpRequestException exception)
            {
                IpGeolocationLog.ProviderTransportFailed(logger, exception, provider.GetType().Name);
            }
        }

        return null;
    }
}
