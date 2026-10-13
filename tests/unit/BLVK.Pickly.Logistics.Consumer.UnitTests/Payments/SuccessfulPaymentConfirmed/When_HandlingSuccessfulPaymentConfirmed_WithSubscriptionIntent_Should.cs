using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.SuccessfulPaymentConfirmed;

public sealed class When_HandlingSuccessfulPaymentConfirmed_WithSubscriptionIntent_Should
{
    [Fact]
    public async Task SendActivateSubscriptionCommand()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var paymentTransactionId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var currencyId = Guid.CreateVersion7();

        await context.CreateSuccessfulPaymentConfirmedHandler().HandleAsync(
            new Contracts.Events.SuccessfulPaymentConfirmed
                {
                    PaymentTransactionId = paymentTransactionId,
                    MerchantReference = "merchant-ref",
                PaymentIntent = PaymentIntent.Subscription,
                PaymentProviderId = Guid.CreateVersion7(),
                Amount = 1500m,
                CurrencyId = currencyId,
                StakeholderId = stakeholderId,
                TenantId = tenantId,
                FlowId = "flow-123"
            },
            CancellationToken.None);

        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<ActivateSubscriptionCommand>(command =>
                command.PaymentTransactionId == paymentTransactionId &&
                command.MerchantReference == "merchant-ref" &&
                command.Amount == 1500m &&
                command.CurrencyId == currencyId &&
                command.StakeholderId == stakeholderId &&
                command.TenantId == tenantId &&
                command.FlowId == "flow-123"),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
