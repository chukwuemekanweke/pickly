using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Specifications;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_FilteringPendingPaymentReconciliations_WithTransactionsOlderThanMaximumAge_Should
{
    [Fact]
    public void ExcludeOlderTransactions()
    {
        var now = new DateTimeOffset(2026, 5, 2, 12, 0, 0, TimeSpan.Zero);
        var withinWindowTransaction = CreateInitiatedTransaction(now.AddHours(-47));
        var olderTransaction = CreateInitiatedTransaction(now.AddHours(-49));
        var tooRecentTransaction = CreateInitiatedTransaction(now.AddMinutes(-1));

        var transactions = new[]
        {
            withinWindowTransaction,
            olderTransaction,
            tooRecentTransaction
        }.AsQueryable();

        var specification = new PendingPaymentReconciliationSpecification(
            now.AddHours(-48),
            now.AddMinutes(-5),
            now.AddMinutes(-2),
            50);

        var result = SpecificationEvaluator.GetQuery(transactions, specification).ToList();

        result.ShouldBe([withinWindowTransaction]);
    }

    private static PaymentTransaction CreateInitiatedTransaction(DateTimeOffset createdAtUtc)
    {
        var transaction = PaymentTransaction.Create($"pay_{Guid.CreateVersion7():N}", PaymentIntent.WalletTopUp, Guid.CreateVersion7(), 1000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        transaction.MarkInitiated("provider-ref", null, null, "payment_initiated");
        transaction.SetCreatedAudit(createdAtUtc, "system");
        return transaction;
    }
}

