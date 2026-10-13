using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessPaymentWebhook;
using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ProcessSafeHavenWebhook;

public sealed class When_ProcessingSafeHavenVirtualAccountTransferWebhook_WithRecognizedTransaction_Should
{
    [Fact]
    public async Task PersistWebhook()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("SafeHaven", PaymentProviderKeys.SafeHaven);
        PaymentWebhookInbox? capturedInbox = null;
        var transaction = PaymentTransaction.Create("payment-ref", PaymentIntent.WalletTopUp, provider.Id, 1000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentWebhookInbox>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.PaymentWebhookInboxRepository.AddAsync(Arg.Do<PaymentWebhookInbox>(inbox => capturedInbox = inbox), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.PaymentTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        var result = await context.CreateSafeHavenVirtualAccountTransferWebhookHandler().HandleAsync(
            new ProcessSafeHavenWebhookCommand<SafeHavenVirtualAccountTransferWebhookData>(
                new SafeHavenWebhook<SafeHavenVirtualAccountTransferWebhookData>(
                    SafeHavenWebhookEvents.VirtualAccountTransfer,
                    new SafeHavenVirtualAccountTransferWebhookData(
                        "provider-ref", "client", "virtual-account", "session", "name-ref", "payment-ref", false, null,
                        "safehaven", "bank", "001", "090286", "Credit Name", "1234567890", null, "1", "Debit Name",
                        "0987654321", null, "1", null, null, 1000m, 0m, 0m, 0m, null, null, "success", false,
                        context.Clock.GetUtcNow(), null, context.Clock.GetUtcNow())),
                "{\"type\":\"virtualAccount.transfer\"}"),
            CancellationToken.None);

        result.Status.ShouldBe(WebhookReceiptStatus.Persisted);
        capturedInbox.ShouldNotBeNull();
        capturedInbox.WebhookEventName.ShouldBe(SafeHavenWebhookEvents.VirtualAccountTransfer);
        capturedInbox.MerchantReference.ShouldBe("payment-ref");
        capturedInbox.ProviderReference.ShouldBe("provider-ref");
    }
}

