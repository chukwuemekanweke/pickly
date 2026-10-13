using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class ProviderByTypeAndKeySpecification : Specification<Provider>
{
    public ProviderByTypeAndKeySpecification(ProviderType providerType, string providerKey)
    {
        Where(provider =>
            provider.ProviderType == providerType &&
            provider.ProviderKey == providerKey);
    }
}
