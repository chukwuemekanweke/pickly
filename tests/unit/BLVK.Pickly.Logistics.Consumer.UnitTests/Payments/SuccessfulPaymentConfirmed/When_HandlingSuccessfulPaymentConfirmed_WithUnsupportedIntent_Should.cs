using Chidelu.Integration.Messaging.RabbitMQ.Core.Exceptions;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.SuccessfulPaymentConfirmed;

public sealed class When_HandlingSuccessfulPaymentConfirmed_WithUnsupportedIntent_Should
{
    [Fact]
    public async Task ThrowCannotProcessMessageNonTransientException()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();

        await Should.ThrowAsync<CannotProcessMessageNonTransientException>(() =>
            context.CreateSuccessfulPaymentConfirmedHandler().HandleAsync(
                new Contracts.Events.SuccessfulPaymentConfirmed
                {
                    PaymentTransactionId = Guid.CreateVersion7(),
                    MerchantReference = "merchant-ref",
                    PaymentIntent = (Contracts.Payments.PaymentIntent)999,
                    PaymentProviderId = Guid.CreateVersion7(),
                    Amount = 1500m,
                    CurrencyId = Guid.CreateVersion7(),
                    StakeholderId = Guid.CreateVersion7(),
                    TenantId = Guid.CreateVersion7()
                },
                CancellationToken.None));
    }
}
