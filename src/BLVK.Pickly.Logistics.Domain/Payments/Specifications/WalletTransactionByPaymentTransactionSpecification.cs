using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class WalletTransactionByPaymentTransactionSpecification : Specification<WalletTransaction>
{
    public WalletTransactionByPaymentTransactionSpecification(Guid paymentTransactionId)
    {
        Where(transaction => transaction.PaymentTransactionId == paymentTransactionId);
    }
}
