using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class PaymentTransactionByProviderReferenceSpecification : Specification<PaymentTransaction>
{
    public PaymentTransactionByProviderReferenceSpecification(string providerReference)
    {
        Where(transaction => transaction.ProviderReference == providerReference);
        EnableTracking();
    }
}
