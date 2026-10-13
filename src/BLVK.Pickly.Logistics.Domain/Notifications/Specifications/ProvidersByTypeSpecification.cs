using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class ProvidersByTypeSpecification : Specification<Provider>
{
    public ProvidersByTypeSpecification(ProviderType providerType)
    {
        Where(provider => provider.ProviderType == providerType);
    }
}
