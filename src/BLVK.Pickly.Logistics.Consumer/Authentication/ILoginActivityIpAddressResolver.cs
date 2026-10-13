namespace BLVK.Pickly.Logistics.Consumer.Authentication;

public interface ILoginActivityIpAddressResolver
{
    Task<LoginActivityIpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken);
}
