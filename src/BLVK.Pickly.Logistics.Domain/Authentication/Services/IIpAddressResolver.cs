namespace BLVK.Pickly.Logistics.Domain.Authentication.Services;

public interface IIpAddressResolver
{
    Task<IpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken);
}
