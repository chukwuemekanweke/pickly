using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessSafeHavenWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.Webhooks.SafeHaven;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.Webhooks.SafeHaven;

public sealed class When_ReceivingSafeHavenVirtualAccountTransferWebhook_WithKnownEvent_Should
{
    [Fact]
    public async Task ReturnOk()
    {
        var context = new PaymentsControllerTestContext();
        var provider = context.CreatePaymentProvider("SafeHaven", PaymentProviderKeys.SafeHaven);
        var transaction = PaymentTransaction.Create("payment-ref", PaymentIntent.WalletTopUp, provider.Id, 1000m, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentWebhookInbox>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.PaymentTransactionRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentTransaction>>(), Arg.Any<CancellationToken>())
            .Returns(transaction);

        var payload = new
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
            createdAt = context.Clock.GetUtcNow(),
            declinedAt = (DateTimeOffset?)null,
            updatedAt = context.Clock.GetUtcNow()
        };
        var request = new SafeHavenWebhookRequest(
            SafeHavenWebhookEvents.VirtualAccountTransfer,
            JsonSerializer.SerializeToElement(payload));
        var sut = new SafeHavenWebhooksController(
            context.CreateSafeHavenAccountCreditWebhookHandler(),
            context.CreateSafeHavenAccountDebitWebhookHandler(),
            context.CreateSafeHavenVirtualAccountTransferWebhookHandler());

        var result = await sut.Handle(request, CancellationToken.None);

        result.ShouldBeOfType<OkResult>();
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

