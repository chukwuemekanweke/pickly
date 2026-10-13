using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Specifications;

public sealed class PendingPaymentReconciliationSpecification : Specification<PaymentTransaction>
{
    public PendingPaymentReconciliationSpecification(
        DateTimeOffset oldestEligibleCreatedAtUtc,
        DateTimeOffset staleThresholdUtc,
        DateTimeOffset nextCheckThresholdUtc,
        int batchSize)
    {
        Where(transaction =>
            transaction.PaymentStatus == PaymentStatus.Initiated &&
            transaction.CreatedAtUtc >= oldestEligibleCreatedAtUtc &&
            transaction.CreatedAtUtc <= staleThresholdUtc &&
            (transaction.LastStatusCheckAtUtc == null || transaction.LastStatusCheckAtUtc <= nextCheckThresholdUtc));
        ApplyOrderBy(transaction => transaction.CreatedAtUtc);
        ApplyPaging(0, batchSize);
        EnableTracking();
    }
}
