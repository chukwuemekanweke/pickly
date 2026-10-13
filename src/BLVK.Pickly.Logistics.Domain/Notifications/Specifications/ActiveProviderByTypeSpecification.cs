using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class ActiveProviderByTypeSpecification : Specification<Provider>
{
    public ActiveProviderByTypeSpecification(ProviderType providerType)
    {
        Where(provider => provider.ProviderType == providerType && provider.IsActive);
    }
}
