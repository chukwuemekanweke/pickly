using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;
using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessPaymentWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ProcessCredoWebhook;

public sealed class When_ProcessingCredoWebhook_WithRecognizedTransaction_Should
{
    [Fact]
    public async Task PersistWebhook()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);
        PaymentWebhookInbox? capturedInbox = null;
        var transaction = PaymentTransaction.Create("merchant-ref", PaymentIntent.WalletTopUp, provider.Id, 1000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentWebhookInbox>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.PaymentWebhookInboxRepository.AddAsync(Arg.Do<PaymentWebhookInbox>(inbox => capturedInbox = inbox), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.PaymentTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        context.CredoWebhookSignatureValidator.ValidateAsync(Arg.Any<CredoWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Valid, "validated"));

        var result = await context.CreateCredoWebhookHandler().HandleAsync(
            new ProcessCredoWebhookCommand(
                new CredoWebhook(
                    CredoWebhookEvents.TransactionSuccessful,
                    new CredoWebhookData(
                        "700607002190001",
                        "trans-ref",
                        "merchant-ref",
                        1000m,
                        1000m,
                        15m,
                        985m,
                        "customer@example.com",
                        "May 7, 2023, 1:37:53 AM",
                        1,
                        "NGN",
                        0,
                        "MasterCard",
                        "Card",
                        new CredoWebhookCustomer("customer@example.com", "John", "Doe", "23470122199999"))),
                "{\"event\":\"transaction.successful\"}",
                "valid-signature"),
            CancellationToken.None);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        capturedInbox.ShouldNotBeNull();
        capturedInbox.WebhookEventName.ShouldBe(CredoWebhookEvents.TransactionSuccessful);
        capturedInbox.MerchantReference.ShouldBe("merchant-ref");
        capturedInbox.ProviderReference.ShouldBe("trans-ref");
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

