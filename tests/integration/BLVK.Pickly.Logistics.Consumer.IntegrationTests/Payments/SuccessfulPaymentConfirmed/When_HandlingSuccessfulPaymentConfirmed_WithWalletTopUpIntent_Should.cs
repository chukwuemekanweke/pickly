using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Consumer.Payments;
using BLVK.Pickly.Logistics.Contracts.Commands.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Payments.SuccessfulPaymentConfirmed;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingSuccessfulPaymentConfirmed_WithWalletTopUpIntent_Should(ContainersFixture fixture)
    : ConsumerWorkerIntegrationTestBase(fixture)
{
    private Guid _paymentTransactionId;
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _currencyId;

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
        var outboxMessages = await scope.DbContext.OutboxMessages
            .Where(message =>
                message.Kind == OutboxMessageKind.Command &&
                message.Type == typeof(CreditWalletCommand).FullName! &&
                message.Payload.Contains(_paymentTransactionId.ToString()))
            .ToListAsync();

        if (outboxMessages.Count > 0)
        {
            scope.DbContext.OutboxMessages.RemoveRange(outboxMessages);
            await scope.DbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task QueueCreditWalletCommand()
    {
        await WhenPublishingSuccessfulPaymentConfirmed();
        await ThenTheCreditWalletCommandIsQueued();

        async Task WhenPublishingSuccessfulPaymentConfirmed()
        {
            using var scope = CreateScope();
            var messageContext = scope.ServiceProvider.GetRequiredService<Chidelu.Integration.Messaging.RabbitMQ.Consumer.IMessageContext>();

            await scope.ServiceProvider.GetRequiredService<SuccessfulPaymentConfirmedHandler>().HandleAsync(
                new Contracts.Events.SuccessfulPaymentConfirmed
                {
                    PaymentTransactionId = _paymentTransactionId,
                    MerchantReference = "merchant-ref",
                    PaymentIntent = Contracts.Payments.PaymentIntent.WalletTopUp,
                    PaymentProviderId = Guid.CreateVersion7(),
                    Amount = 2500m,
                    CurrencyId = _currencyId,
                    StakeholderId = _stakeholderId,
                    TenantId = _tenantId,
                    FlowId = "flow-123"
                },
                CancellationToken.None);
        }

        async Task ThenTheCreditWalletCommandIsQueued()
        {
            await WaitForConditionAsync(async () =>
            {
                using var scope = CreateDbContextScope();
                return await scope.DbContext.OutboxMessages.AnyAsync(message =>
                    message.Kind == OutboxMessageKind.Command &&
                    message.Type == typeof(CreditWalletCommand).FullName! &&
                    message.Payload.Contains(_paymentTransactionId.ToString()));
            });

            using var scope = CreateDbContextScope();
            var outboxMessage = await scope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Command &&
                    message.Type == typeof(CreditWalletCommand).FullName! &&
                    message.Payload.Contains(_paymentTransactionId.ToString()))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstAsync();

            var command = JsonSerializer.Deserialize<CreditWalletCommand>(outboxMessage.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            command.ShouldNotBeNull();
            command.PaymentTransactionId.ShouldBe(_paymentTransactionId);
            command.StakeholderId.ShouldBe(_stakeholderId);
            command.TenantId.ShouldBe(_tenantId);
            command.CurrencyId.ShouldBe(_currencyId);
        }
    }
}
