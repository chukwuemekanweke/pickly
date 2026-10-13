using BLVK.Pickly.Logistics.Application.Payments.Features.ProcessCredoWebhook;
using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.ProcessCredoWebhook;

public sealed class When_ProcessingCredoWebhook_WithMissingEventName_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsFlowTestContext();
        var provider = context.CreatePaymentProvider("Credo", PaymentProviderKeys.Credo);

        context.PaymentProviderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Domain.Payments.Entities.PaymentProvider>>(), Arg.Any<CancellationToken>())
            .Returns(provider);
        context.CredoWebhookSignatureValidator.ValidateAsync(Arg.Any<CredoWebhookSignatureValidationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Valid, null));

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateCredoWebhookHandler().HandleAsync(
                new ProcessCredoWebhookCommand(
                    new CredoWebhook(
                        " ",
                        new CredoWebhookData(
                            "700607002190001",
                            "trans-ref",
                            "merchant-ref",
                            1000m,
                            1000m,
                            15m,
                            985m,
                            "customer@example.com",
                            "May 7, 2023, 1:37:53 AM",
                            1,
                            "NGN",
                            0,
                            "MasterCard",
                            "Card",
                            new CredoWebhookCustomer("customer@example.com", "John", "Doe", "23470122199999"))),
                    "{}",
                    "valid-signature"),
                CancellationToken.None));

        exception.Message.ShouldContain("required");
    }
}
