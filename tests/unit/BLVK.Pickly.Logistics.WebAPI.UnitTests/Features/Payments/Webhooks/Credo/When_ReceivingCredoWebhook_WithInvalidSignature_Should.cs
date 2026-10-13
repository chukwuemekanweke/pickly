using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.Webhooks.Credo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.Webhooks.Credo;

public sealed class When_ReceivingCredoWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnUnauthorized()
    {
        var context = new PaymentsControllerTestContext();
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.PaymentWebhookInboxRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<PaymentWebhookInbox>>(), Arg.Any<CancellationToken>())
            .Returns((PaymentWebhookInbox?)null);
        context.CredoWebhookSignatureValidator.ValidateAsync(Arg.Any<CredoWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, KnownWebhookStatusChangeReasons.Shared.InvalidSignature));

        var request = new CredoWebhookRequest(
            CredoWebhookEvents.TransactionSuccessful,
            JsonSerializer.SerializeToElement(new
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
            }));
        var sut = new CredoWebhooksController(context.CreateCredoWebhookHandler());
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        sut.ControllerContext.HttpContext.Request.Headers["X-Credo-Signature"] = "invalid-signature";

        var result = await sut.Handle(request, CancellationToken.None);

        var unauthorizedResult = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorizedResult.Value.ShouldBe("Invalid signature");
    }
}
