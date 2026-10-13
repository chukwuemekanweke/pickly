using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_ValidatingCredoWebhook_WithInvalidSignature_Should
{
    [Fact]
    public async Task ReturnInvalidStatus()
    {
        var sut = new CredoWebhookSignatureValidator(
            Options.Create(new CredoOptions
            {
                SecretKey = "test_secret_key"
            }));

        var result = await sut.ValidateAsync(
            new CredoWebhookSignatureValidationRequest(
                "invalid-signature",
                "700607002190001"),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Invalid);
        result.StatusChangeReason.ShouldBe(KnownWebhookStatusChangeReasons.Shared.InvalidSignature);
    }
}
