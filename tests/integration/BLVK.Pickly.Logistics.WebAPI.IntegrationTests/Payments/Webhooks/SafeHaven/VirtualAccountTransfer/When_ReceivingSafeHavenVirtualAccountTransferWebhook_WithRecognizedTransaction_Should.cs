using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Payments.Webhooks.SafeHaven.VirtualAccountTransfer;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReceivingSafeHavenVirtualAccountTransferWebhook_WithRecognizedTransaction_Should(ContainersFixture fixture)
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
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.PaymentWebhooks.SafeHaven.V1,
                new
                {
                    type = SafeHavenWebhookEvents.VirtualAccountTransfer,
                    data = new
                    {
                        _id = "provider-ref",
                        client = "client",
                        virtualAccount = "virtual-account",
                        sessionId = "session",
                        nameEnquiryReference = "name-ref",
                        paymentReference = "payment-ref",
                        isReversed = false,
                        reversalReference = (string?)null,
                        provider = "safehaven",
                        providerChannel = "bank",
                        providerChannelCode = "001",
                        destinationInstitutionCode = "090286",
                        creditAccountName = "Credit Name",
                        creditAccountNumber = "1234567890",
                        creditBankVerificationNumber = (string?)null,
                        creditKYCLevel = "1",
                        debitAccountName = "Debit Name",
                        debitAccountNumber = "0987654321",
                        debitBankVerificationNumber = (string?)null,
                        debitKYCLevel = "1",
                        transactionLocation = (string?)null,
                        narration = (string?)null,
                        amount = 1000m,
                        fees = 0m,
                        vat = 0m,
                        stampDuty = 0m,
                        responseCode = (string?)null,
                        responseMessage = (string?)null,
                        status = "success",
                        isDeleted = false,
                        createdAt = DateTimeOffset.UtcNow,
                        declinedAt = (DateTimeOffset?)null,
                        updatedAt = DateTimeOffset.UtcNow
                    }
                });
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
            inbox.MerchantReference.ShouldBe("payment-ref");
            inbox.WebhookEventName.ShouldBe(SafeHavenWebhookEvents.VirtualAccountTransfer);
            inbox.WebhookProcessingStatus.ShouldBe(WebhookProcessingStatus.Received);
        }
    }

    private async Task SeedPaymentSetupAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var provider = PaymentProvider.Create("SafeHaven", "safehaven", true);
        var transaction = PaymentTransaction.Create(
            "payment-ref",
            Contracts.Payments.PaymentIntent.WalletTopUp,
            provider.Id,
            1000m,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7());
        transaction.MarkInitiated("provider-ref", null, null, "payment_initiated");

        await dbContext.PaymentProviders.AddAsync(provider);
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
}
