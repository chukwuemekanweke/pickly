using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Consumer.Payments;
using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Payments.ActivateSubscription;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingActivateSubscription_WithNewPayment_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _paymentTransactionId;
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _currencyId;
    private Guid _subscriptionActivationId;

    protected override Task InitializeWorkerTestAsync()
    {
        _paymentTransactionId = Guid.CreateVersion7();
        _tenantId = Guid.CreateVersion7();
        _stakeholderId = Guid.CreateVersion7();
        _currencyId = Guid.CreateVersion7();
        return Task.CompletedTask;
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        using var scope = CreateDbContextScope();
        var activation = await scope.DbContext.SubscriptionActivations.FirstOrDefaultAsync(item => item.Id == _subscriptionActivationId);
        if (activation is not null)
        {
            scope.DbContext.SubscriptionActivations.Remove(activation);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CreateSubscriptionActivation()
    {
        await WhenPublishingActivateSubscriptionCommand();
        await ThenTheSubscriptionActivationIsCreated();

        async Task WhenPublishingActivateSubscriptionCommand()
        {
            using var scope = CreateScope();
            var messageContext = scope.ServiceProvider.GetRequiredService<Chidelu.Integration.Messaging.RabbitMQ.Consumer.IMessageContext>();

            await scope.ServiceProvider.GetRequiredService<ActivateSubscriptionHandler>().HandleAsync(
                new ActivateSubscriptionCommand(_paymentTransactionId, "merchant-ref", 5000m, _currencyId)
                {
                    StakeholderId = _stakeholderId,
                    TenantId = _tenantId,
                    FlowId = "flow-123"
                },
                CancellationToken.None);
        }

        async Task ThenTheSubscriptionActivationIsCreated()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                return await scope.DbContext.SubscriptionActivations.AnyAsync(item => item.PaymentTransactionId == _paymentTransactionId);
            });

            using var scope = CreateDbContextScope();
            var activation = await scope.DbContext.SubscriptionActivations.FirstAsync(item => item.PaymentTransactionId == _paymentTransactionId);

            _subscriptionActivationId = activation.Id;
            activation.StakeholderId.ShouldBe(_stakeholderId);
            activation.TenantId.ShouldBe(_tenantId);
            activation.Amount.ShouldBe(5000m);
        }
    }
}
