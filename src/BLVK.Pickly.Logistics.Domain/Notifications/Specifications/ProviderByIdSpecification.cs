using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class ProviderByIdSpecification : Specification<Provider>
{
    public ProviderByIdSpecification(Guid providerId)
    {
        Where(provider => provider.Id == providerId);
    }
}
