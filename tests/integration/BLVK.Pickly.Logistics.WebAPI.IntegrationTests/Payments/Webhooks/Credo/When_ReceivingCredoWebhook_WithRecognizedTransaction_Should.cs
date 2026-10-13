using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Payments.Webhooks.Credo;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReceivingCredoWebhook_WithRecognizedTransaction_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private Guid _paymentProviderId;
    private Guid _paymentTransactionId;
    private Guid _webhookInboxId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await SeedPaymentSetupAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task PersistWebhook()
    {
        await WhenPostingWebhook();
        await ThenTheWebhookIsPersisted();

        async Task WhenPostingWebhook()
        {
            var payload = new
            {
                @event = CredoWebhookEvents.TransactionSuccessful,
                data = new
                {
                    businessCode = "700607002190001",
                    transRef = "trans-ref",
                    businessRef = "merchant-ref",
                    debitedAmount = 1000m,
                    transAmount = 1000m,
                    transFeeAmount = 15m,
                    settlementAmount = 985m,
                    customerId = "customer@example.com",
                    transactionDate = "May 7, 2023, 1:37:53 AM",
                    channelId = 1,
                    currencyCode = "NGN",
                    status = 0,
                    paymentMethodType = "MasterCard",
                    paymentMethod = "Card",
                    customer = new
                    {
                        customerEmail = "customer@example.com",
                        firstName = "John",
                        lastName = "Doe",
                        phoneNo = "23470122199999"
                    }
                }
            };
            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointUrl.PaymentWebhooks.Credo.V1)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("X-Credo-Signature", ComputeSignature("test_secret_key", "700607002190001"));

            _response = await Client.SendAsync(request);
        }

        async Task ThenTheWebhookIsPersisted()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
            var inbox = await dbContext.PaymentWebhookInboxes
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstAsync(item => item.PaymentProviderId == _paymentProviderId);

            _webhookInboxId = inbox.Id;
            inbox.MerchantReference.ShouldBe("merchant-ref");
            inbox.WebhookEventName.ShouldBe(CredoWebhookEvents.TransactionSuccessful);
            inbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Received);
        }
    }

    private async Task SeedPaymentSetupAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var provider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.ProviderKey == "credo")
            ?? PaymentProvider.Create("Credo", "credo", true);
        var transaction = PaymentTransaction.Create(
            "merchant-ref",
            Contracts.Payments.PaymentIntent.WalletTopUp,
            provider.Id,
            1000m,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("trans-ref", null, null, "payment_initiated");

        if (dbContext.Entry(provider).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            await dbContext.PaymentProviders.AddAsync(provider);
        }
        await dbContext.PaymentTransactions.AddAsync(transaction);
        await dbContext.SaveChangesAsync();

        _paymentProviderId = provider.Id;
        _paymentTransactionId = transaction.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();

        if (_webhookInboxId != Guid.Empty)
        {
            var inbox = await dbContext.PaymentWebhookInboxes.FirstOrDefaultAsync(item => item.Id == _webhookInboxId);
            if (inbox is not null)
            {
                dbContext.PaymentWebhookInboxes.Remove(inbox);
            }
        }

        var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(item => item.Id == _paymentTransactionId);
        if (transaction is not null)
        {
            dbContext.PaymentTransactions.Remove(transaction);
        }

        var provider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.Id == _paymentProviderId);
        if (provider is not null)
        {
            dbContext.PaymentProviders.Remove(provider);
        }

        await dbContext.SaveChangesAsync();
    }

    private static string ComputeSignature(string secretKey, string businessCode)
    {
        using var sha512 = SHA512.Create();
        var hash = sha512.ComputeHash(Encoding.UTF8.GetBytes(secretKey + businessCode));

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }
}
