using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class SubscriptionActivationByPaymentTransactionSpecification : Specification<SubscriptionActivation>
{
    public SubscriptionActivationByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(activation => activation.PaymentTransactionId == paymentTransactionId);
    }
}
