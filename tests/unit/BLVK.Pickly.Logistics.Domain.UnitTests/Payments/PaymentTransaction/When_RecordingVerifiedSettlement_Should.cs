using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Payments.PaymentTransactions;

public sealed class When_RecordingVerifiedSettlement_Should
{
    [Fact]
    public void RejectAmountOrCurrencyMismatch()
    {
        var currencyId = Guid.CreateVersion7();
        var transaction = CreateInitiated(PaymentIntent.WalletTopUp, currencyId);

        Should.Throw<AggregateStateException>(() =>
            transaction.ConfirmVerifiedSettlement("provider-ref", 999m, currencyId, DateTimeOffset.UtcNow, "verified"));
        Should.Throw<AggregateStateException>(() =>
            transaction.ConfirmVerifiedSettlement("provider-ref", 1000m, Guid.CreateVersion7(), DateTimeOffset.UtcNow, "verified"));
    }

    [Fact]
    public void PreserveUnresolvedEvidenceAndPreventAutomaticSuccess()
    {
        var transaction = CreateInitiated(PaymentIntent.DeliveryPayment, Guid.CreateVersion7());

        transaction.MarkUnresolved(
            "provider-ref",
            300m,
            null,
            "USD",
            DateTimeOffset.UtcNow,
            "Amount and currency mismatch.",
            "unresolved");

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Unresolved);
        transaction.VerifiedAmount.ShouldBe(300m);
        transaction.VerifiedCurrencyCode.ShouldBe("USD");
        transaction.FailureReason.ShouldBe("Amount and currency mismatch.");
        Should.Throw<AggregateStateException>(() =>
            transaction.ConfirmVerifiedSettlement(
                "provider-ref",
                transaction.Amount,
                transaction.CurrencyId,
                DateTimeOffset.UtcNow,
                "verified"));
    }

    [Fact]
    public void RestrictLateSettlementToDeliveryFailuresAndExpirations()
    {
        var transaction = CreateInitiated(PaymentIntent.WalletTopUp, Guid.CreateVersion7());
        transaction.MarkFailed("provider-ref", "failed", "failed", DateTimeOffset.UtcNow);

        Should.Throw<AggregateStateException>(() =>
            transaction.ConfirmLateVerifiedSettlement(
                "provider-ref",
                transaction.Amount,
                transaction.CurrencyId,
                DateTimeOffset.UtcNow,
                "late"));
    }

    private static PaymentTransaction CreateInitiated(PaymentIntent intent, Guid currencyId)
    {
        var transaction = PaymentTransaction.Create(
            "merchant-ref",
            intent,
            Guid.CreateVersion7(),
            1000m,
            currencyId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("provider-ref", null, null, "initiated");
        return transaction;
    }
}
