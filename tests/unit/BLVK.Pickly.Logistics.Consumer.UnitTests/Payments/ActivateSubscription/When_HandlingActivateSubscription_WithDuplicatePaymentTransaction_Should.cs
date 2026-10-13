using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Consumer.UnitTests.Payments.ActivateSubscription;

public sealed class When_HandlingActivateSubscription_WithDuplicatePaymentTransaction_Should
{
    [Fact]
    public async Task IgnoreMessage()
    {
        var context = new PaymentsConsumerTestContext();
        context.SetCorrelationId();
        var command = context.CreateActivateSubscriptionCommand(5000m, Guid.CreateVersion7());

        context.SubscriptionActivationRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<SubscriptionActivation>>(), Arg.Any<CancellationToken>())
            .Returns(SubscriptionActivation.Create(
                command.PaymentTransactionId,
                command.StakeholderId!.Value,
                command.TenantId,
                null,
                command.Amount,
                command.CurrencyId,
                context.Clock.GetUtcNow()));

        await context.CreateActivateSubscriptionHandler().HandleAsync(command, CancellationToken.None);

        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
