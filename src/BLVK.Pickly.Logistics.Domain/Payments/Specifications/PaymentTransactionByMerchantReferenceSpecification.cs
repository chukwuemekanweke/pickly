using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class PaymentTransactionByMerchantReferenceSpecification : Specification<PaymentTransaction>
{
    public PaymentTransactionByMerchantReferenceSpecification(string merchantReference)
    {
        Where(transaction => transaction.MerchantReference == merchantReference);
        EnableTracking();
    }
}
