using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ReconcilePayments;

public sealed class When_ReconcilingPayments_WithProcessingVerification_Should
{
    [Fact]
    public async Task RecordStatusCheck()
    {
        var context = new PaymentsFlowTestContext();
        var currency = context.CreateCurrency("NGN");
        var provider = context.CreatePaymentProvider("SafeHaven", PaymentProviderKeys.SafeHaven);
        var paymentProviderService = Substitute.For<IPaymentProviderService>();
        var transaction = PaymentTransaction.Create("merchant-processing", PaymentIntent.Subscription, provider.Id, 1000m, currency.Id, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        transaction.MarkInitiated("provider-ref", null, null, KnownPaymentTransactionChangeReasons.PaymentInitiated);

        context.PaymentTransactionRepository.ListAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns([transaction]);
        context.CurrencyRepository.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>())
            .Returns(currency);
        context.PaymentProviderRepository.GetByIdAsync(provider.Id, Arg.Any<CancellationToken>())
            .Returns(provider);
        paymentProviderService.ProviderKey.Returns(PaymentProviderKeys.SafeHaven);
        paymentProviderService.VerifyPaymentAsync(Arg.Any<PaymentProviderVerificationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderVerificationResult(
                PaymentProviderVerificationStatus.Processing,
                "provider-ref",
                null,
                KnownPaymentTransactionChangeReasons.ReconciliationStillProcessing));
        context.PaymentProviderServices.Add(paymentProviderService);

        await context.CreatePaymentReconciliationService().HandleAsync(
            context.Clock.GetUtcNow().AddHours(-48),
            context.Clock.GetUtcNow().AddMinutes(-10),
            context.Clock.GetUtcNow().AddMinutes(-2),
            50,
            CancellationToken.None);

        transaction.PaymentStatus.ShouldBe(PaymentStatus.Initiated);
        transaction.LastStatusCheckAtUtc.ShouldBe(context.Clock.GetUtcNow());
        await context.EventPublisher.DidNotReceive().PublishAsync(Arg.Any<SuccessfulPaymentConfirmed>(), Arg.Any<CancellationToken>());
        await context.CommandSender.DidNotReceive().SendAsync(
            Arg.Any<SendNotificationCommand>(),
            Arg.Any<CancellationToken>());
    }
}

