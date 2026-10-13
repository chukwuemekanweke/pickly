using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Authentication.Specifications;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;

namespace BLVK.Pickly.Logistics.Infrastructure.Authentication;

public sealed class IpAddressResolver(IRepository<IpAddress> repository) : IIpAddressResolver
{
    public async Task<IpAddressResolution> ResolveAsync(string ipAddress, CancellationToken cancellationToken)
    {
        ipAddress = string.IsNullOrWhiteSpace(ipAddress) ? "0.0.0.0" : ipAddress.Trim();
        var address = await repository.FirstOrDefaultAsync(new IpAddressByValueSpecification(ipAddress), cancellationToken);
        if (address is null)
        {
            address = IpAddress.Create(ipAddress);
            await repository.AddAsync(address, cancellationToken);
        }

        return new IpAddressResolution(address.Id, address.GetCurrentLocation()?.Id);
    }
}
