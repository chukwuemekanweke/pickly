using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ReconcilePayments;

public sealed class When_ReconcilingPayments_WithExpiredVerification_Should
{
    [Fact]
    public async Task MarkTransactionExpired()
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create("merchant-expired", PaymentIntent.WalletTopUp, provider.Id, 1000m, currency.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        transaction.MarkInitiated("provider-ref", null, null, KnownPaymentTransactionChangeReasons.PaymentInitiated);
        transaction.SetPaymentMethodType(PaymentMethodType.PaymentLink);
        transaction.RecordStatusCheck(context.Clock.GetUtcNow().AddHours(-25));

        context.PaymentTransactionRepository.ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns([transaction]);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(provider);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.Credo);
        paymentProviderService.VerifyPaymentAsync(Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Expired,
                "provider-ref",
                "provider_reported_expired",
                KnownPaymentTransactionChangeReasons.ReconciliationConfirmedExpired));
        context.PaymentProviderServices.Add(paymentProviderService);

        await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddHours(-48),
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Expired);
        transaction.FailureReason.ShouldBe("provider_reported_expired");
        transaction.StatusChangeReason.ShouldBe(KnownPaymentTransactionChangeReasons.ReconciliationConfirmedExpired);
        transaction.LastStatusCheckAtUtc.ShouldBe(context.Clock.GetUtcNow());
        await context.EventPublisher.DidNotReceive().PublishAsync(Arg.Any<SuccessfulPaymentConfirmed>(), Arg.Any<CancellationToken>());
    }
}

