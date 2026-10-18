using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ReconcilePayments;

public sealed class When_ReconcilingPayments_WithUnverifiedSettlement_Should
{
    public static TheoryData<decimal?, string?, string> Discrepancies => new()
    {
        { 3000m, "NGN", "verified 3000 NGN" },
        { 5000m, "USD", "verified 5000 USD" },
        { null, "NGN", "verified missing NGN" },
        { 5000m, null, "verified 5000 missing" }
    };

    [Theory]
    [MemberData(nameof(Discrepancies))]
    public async Task MarkTransactionUnresolvedAndSuppressSuccessEvent(
        decimal? verifiedAmount,
        string? verifiedCurrencyCode,
        string expectedReasonFragment)
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var providerService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create(
            "delivery-unresolved",
            PaymentIntent.DeliveryPayment,
            provider.Id,
            5000m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("provider-ref", null, null, KnownPaymentTransactionChangeReasons.PaymentInitiated);
        var attempt = DeliveryPaymentAttempt.Create(Guid.CreateVersion7(), transaction.Id, "attempt-key");

        ConfigureReconciliation(
            context,
            currency,
            provider,
            providerService,
            transaction,
            new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                "provider-ref",
                verifiedAmount,
                verifiedCurrencyCode,
                context.Clock.GetUtcNow(),
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess));
        context.DeliveryPaymentAttemptRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<DeliveryPaymentAttempt>>(), Arg.Any<CancellationToken>())
            .Returns(attempt);

        await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddHours(-48),
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Unresolved);
        transaction.VerifiedAmount.ShouldBe(verifiedAmount);
        transaction.VerifiedCurrencyCode.ShouldBe(verifiedCurrencyCode);
        transaction.FailureReason!.ShouldContain(expectedReasonFragment);
        transaction.StatusChangeReason.ShouldBe(KnownPaymentTransactionChangeReasons.ReconciliationUnresolvedSettlement);
        attempt.ApplicationException.ShouldBe(transaction.FailureReason);
        await context.EventPublisher.DidNotReceive().PublishAsync(
            Arg.Any<SuccessfulPaymentConfirmed>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Expired)]
    public async Task ConfirmGenuineLateDeliverySettlement(PaymentStatus previousStatus)
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var providerService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create(
            "delivery-late-settlement",
            PaymentIntent.DeliveryPayment,
            provider.Id,
            5000m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("provider-ref", null, null, KnownPaymentTransactionChangeReasons.PaymentInitiated);
        if (previousStatus == PaymentStatus.Failed)
        {
            transaction.MarkFailed("provider-ref", "failed", "failed", context.Clock.GetUtcNow().AddHours(-1));
        }
        else
        {
            transaction.MarkExpired("provider-ref", "expired", "expired");
        }

        ConfigureReconciliation(
            context,
            currency,
            provider,
            providerService,
            transaction,
            new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Succeeded,
                "provider-ref",
                5000m,
                "NGN",
                context.Clock.GetUtcNow(),
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedSuccess));

        await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddHours(-48),
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Succeeded);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<SuccessfulPaymentConfirmed>(message => message.PaymentTransactionId == transaction.Id),
            Arg.Any<CancellationToken>());
    }

    private static void ConfigureReconciliation(
        PaymentsFlowTestContext context,
        Currency currency,
        PaymentProvider provider,
        IPaymentProviderService providerService,
        PaymentTransaction transaction,
        PaymentProviderVerificationResult verificationResult)
    {
        context.PaymentTransactionRepository.ListAsync(
                Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns([transaction]);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>()).Returns(currency);
        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>()).Returns(provider);
        providerService.ProviderKey.Returns(provider.ProviderKey);
        providerService.VerifyPaymentAsync(
                Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(verificationResult);
        context.PaymentProviderServices.Add(providerService);
    }
}
