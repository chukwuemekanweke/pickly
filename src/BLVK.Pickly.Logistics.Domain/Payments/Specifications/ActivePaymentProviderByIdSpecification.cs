using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class ActivePaymentProviderByIdSpecification : Specification<PaymentProvider>
{
    public ActivePaymentProviderByIdSpecification(Guid paymentProviderId)
    {
        Where(provider => provider.Id == paymentProviderId && provider.IsActive);
    }
}
