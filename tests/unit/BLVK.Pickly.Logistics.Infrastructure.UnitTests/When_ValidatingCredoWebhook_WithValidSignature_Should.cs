using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_ValidatingCredoWebhook_WithValidSignature_Should
{
    [Fact]
    public async Task ReturnValidStatus()
    {
        var sut = new CredoWebhookSignatureValidator(
            Options.Create(new CredoOptions
            {
                SecretKey = "test_secret_key"
            }));
        var signature = ComputeSignature("test_secret_key", "700607002190001");

        var result = await sut.ValidateAsync(
            new CredoWebhookSignatureValidationRequest(
                signature,
                "700607002190001"),
            CancellationToken.None);

        result.SignatureValidationStatus.ShouldBe(SignatureValidationStatus.Valid);
        result.StatusChangeReason.ShouldBe(KnownWebhookStatusChangeReasons.Shared.SignatureVerified);
    }

    private static string ComputeSignature(string secretKey, string businessCode)
    {
        var hash = SHA512.HashData(Encoding.UTF8.GetBytes($"{secretKey}{businessCode}"));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
