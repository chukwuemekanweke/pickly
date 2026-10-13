using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessPaymentWebhook;
using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ProcessSafeHavenWebhook;

public sealed class When_ProcessingSafeHavenAccountCreditWebhook_WithRecognizedTransaction_Should
{
    [Fact]
    public async Task IgnoreWebhook()
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

        var result = await context.CreateSafeHavenAccountCreditWebhookHandler().HandleAsync(
            new ProcessSafeHavenWebhookCommand<SafeHavenAccountCreditWebhookData>(
                new SafeHavenWebhook<SafeHavenAccountCreditWebhookData>(
                    SafeHavenWebhookEvents.AccountCredit,
                    new SafeHavenAccountCreditWebhookData(
                        false, false, "provider-ref", "client", "account", "credit", "session", "name-ref", "payment-ref",
                        null, false, null, "safehaven", "bank", "001", "090286", "Credit Name", "1234567890", null, "1",
                        "Debit Name", "0987654321", "Real Debit", "1111111111", null, "1", null, null, 1000m, 0m, 0m, 0m,
                        null, null, "success", false, context.Clock.GetUtcNow(), context.Clock.GetUtcNow())),
                "{\"type\":\"account.credit\"}"),
            CancellationToken.None);

        result.Status.ShouldBe(WebhookReceiptStatus.UnidentifiedTransaction);
        capturedInbox.ShouldNotBeNull();
        capturedInbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Ignored);
    }
}

