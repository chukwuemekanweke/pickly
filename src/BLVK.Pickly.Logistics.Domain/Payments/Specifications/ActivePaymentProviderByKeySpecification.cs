using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class ActivePaymentProviderByKeySpecification : Specification<PaymentProvider>
{
    public ActivePaymentProviderByKeySpecification(string providerKey)
    {
        Where(provider => provider.ProviderKey == providerKey && provider.IsActive);
    }
}
