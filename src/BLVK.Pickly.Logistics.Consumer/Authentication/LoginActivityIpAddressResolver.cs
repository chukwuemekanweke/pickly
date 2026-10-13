using BLVK.Pickly.Logistics.Domain.Authentication.Services;

namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public sealed class LoginActivityIpAddressResolver(IIpAddressResolver ipAddressResolver) : ILoginActivityIpAddressResolver
{
    public async Task<LoginActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        var resolved = await ipAddressResolver.ResolveAsync(ipAddress, cancellationToken);
        return new LoginActivityIpAddressResolution(resolved.IpAddressId, resolved.IpAddressLocationId);
    }
}
