using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.SuccessfulPaymentConfirmed;

public sealed class When_HandlingSuccessfulPaymentConfirmed_WithDeliveryPaymentIntent_Should
{
    [Fact]
    public async Task SendConfirmDeliveryPaymentCommand()
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
                MerchantReference = "delivery-payment-ref",
                PaymentIntent = PaymentIntent.DeliveryPayment,
                PaymentProviderId = Guid.CreateVersion7(),
                Amount = 2500m,
                CurrencyId = currencyId,
                StakeholderId = stakeholderId,
                TenantId = tenantId,
                FlowId = "flow-delivery"
            },
            CancellationToken.None);

        await context.CommandSender.Received(1).SendAsync(
            Arg.Is<ConfirmDeliveryPaymentCommand>(command =>
                command.PaymentTransactionId == paymentTransactionId &&
                command.MerchantReference == "delivery-payment-ref" &&
                command.Amount == 2500m &&
                command.CurrencyId == currencyId &&
                command.StakeholderId == stakeholderId &&
                command.TenantId == tenantId),
            Arg.Any<CancellationToken>());
    }
}
