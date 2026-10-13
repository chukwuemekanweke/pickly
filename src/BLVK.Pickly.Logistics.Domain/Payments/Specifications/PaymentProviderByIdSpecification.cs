using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class PaymentProviderByIdSpecification : Specification<PaymentProvider>
{
    public PaymentProviderByIdSpecification(Guid paymentProviderId)
    {
        Where(provider => provider.Id == paymentProviderId);
        EnableTracking();
    }
}
