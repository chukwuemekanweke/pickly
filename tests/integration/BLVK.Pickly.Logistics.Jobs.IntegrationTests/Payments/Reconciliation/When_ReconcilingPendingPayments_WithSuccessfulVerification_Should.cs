using BLVK.Pickly.Logistics.Application;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Payments;
using BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;
using BLVK.Pickly.Logistics.Jobs.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.Payments.Reconciliation;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReconcilingPendingPayments_WithSuccessfulVerification_Should(ContainersFixture fixture)
    : JobsWorkerIntegrationTestBase(fixture)
{
    private readonly WireMockServer _wireMockServer = WireMockServer.Start();
    private Guid _paymentProviderId;
    private Guid _currencyId;
    private Guid _paymentTransactionId;
    private Guid _outboxMessageId;

    [Fact]
    public async Task MarkTransactionAsSucceededAndQueueEvent()
    {
        await WhenTheReconciliationWorkerRuns();
        await ThenThePaymentIsSucceededAndEventIsQueued();

        async Task WhenTheReconciliationWorkerRuns()
        {
            await WaitForConditionAsync(async () =>
            {
                await using var scope = CreateDbContextScope();
                var transaction = await scope.DbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
                return transaction?.PaymentStatus == Contracts.Payments.PaymentStatus.Succeeded;
            });
        }

        async Task ThenThePaymentIsSucceededAndEventIsQueued()
        {
            await using var scope = CreateDbContextScope();
            var transaction = await scope.DbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
            transaction.ShouldNotBeNull();
            transaction.PaymentStatus.ShouldBe(Contracts.Payments.PaymentStatus.Succeeded);

            var outboxMessage = scope.DbContext.OutboxMessages
                .Where(message =>
                    message.Kind == OutboxMessageKind.Event &&
                    message.Type == typeof(SuccessfulPaymentConfirmed).FullName! &&
                    message.Payload.Contains(_paymentTransactionId.ToString()))
                .OrderByDescending(message => message.EnqueuedAtUtc)
                .FirstOrDefault();

            outboxMessage.ShouldNotBeNull();
            _outboxMessageId = outboxMessage.Id;

            var @event = JsonSerializer.Deserialize<SuccessfulPaymentConfirmed>(outboxMessage.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            @event.ShouldNotBeNull();
            @event.PaymentTransactionId.ShouldBe(_paymentTransactionId);
        }
    }

    public override async Task InitializeAsync()
    {
        ConfigureCredoStubs();
        await base.InitializeAsync();
    }

    public override async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _wireMockServer.Dispose();
    }

    protected override async Task InitializeWorkerTestAsync()
    {
        await SeedPendingPaymentAsync();
    }

    protected override async Task DisposeWorkerTestAsync()
    {
        await using var scope = CreateDbContextScope();
        var outboxMessage = await scope.DbContext.OutboxMessages.FindAsync(_outboxMessageId);
        if (outboxMessage is not null)
        {
            scope.DbContext.OutboxMessages.Remove(outboxMessage);
        }

        var transaction = await scope.DbContext.PaymentTransactions.FindAsync(_paymentTransactionId);
        if (transaction is not null)
        {
            scope.DbContext.PaymentTransactions.Remove(transaction);
        }

        var currency = await scope.DbContext.Currencies.FindAsync(_currencyId);
        if (currency is not null)
        {
            scope.DbContext.Currencies.Remove(currency);
        }

        var provider = await scope.DbContext.PaymentProviders.FindAsync(_paymentProviderId);
        if (provider is not null)
        {
            scope.DbContext.PaymentProviders.Remove(provider);
        }

        await scope.DbContext.SaveChangesAsync();
    }

    protected override void RegisterWorkers(IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddPaymentServices(configuration);
        services.AddTransactionalOutbox();
        services.AddPaymentReconciliation(configuration);
    }

    protected override IReadOnlyDictionary<string, string?> GetAdditionalConfiguration() =>
        new Dictionary<string, string?>
        {
            ["Payments:Credo:BaseUrl"] = _wireMockServer.Urls.Single(),
            ["Payments:Credo:PublicKey"] = "0PUB_test_public_key",
            ["Payments:Credo:SecretKey"] = "test_secret_key",
            ["Payments:Credo:CallbackUrl"] = "https://backend.integration.local/payments/webhooks/credo"
        };

    private async Task SeedPendingPaymentAsync()
    {
        await using var scope = CreateDbContextScope();
        var now = TimeProvider.System.GetUtcNow();
        var provider = PaymentProvider.Create("Credo", "credo", true);
        var currency = Currency.Create("NGN", "Naira", true);
        var transaction = PaymentTransaction.Create(
            "merchant-success",
            Contracts.Payments.PaymentIntent.WalletTopUp,
            provider.Id,
            1500m,
            currency.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("provider-ref", null, null, "payment_initiated");
        transaction.RecordStatusCheck(now.AddMinutes(-10));

        await scope.DbContext.PaymentProviders.AddAsync(provider);
        await scope.DbContext.Currencies.AddAsync(currency);
        await scope.DbContext.PaymentTransactions.AddAsync(transaction);
        await scope.DbContext.SaveChangesAsync();

        _paymentProviderId = provider.Id;
        _currencyId = currency.Id;
        _paymentTransactionId = transaction.Id;
    }

    private void ConfigureCredoStubs()
    {
        _wireMockServer
            .Given(Request.Create().WithPath("/transaction/merchant-success/verify").UsingGet())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(System.Net.HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        status = 200,
                        message = "Successfully processed",
                        data = new
                        {
                            businessCode = "700607001390003",
                            transRef = "provider-ref",
                            businessRef = "merchant-success",
                            debitedAmount = 1500m,
                            transAmount = 1500m,
                            transFeeAmount = 0m,
                            settlementAmount = 1500m,
                            customerId = "ada@example.com",
                            transactionDate = "2026-05-01 00:00:00",
                            channelId = 0,
                            currencyCode = "NGN",
                            status = 0,
                            metadata = Array.Empty<object>()
                        },
                        execTime = 0,
                        error = Array.Empty<string>()
                    }));
    }
}
