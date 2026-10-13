using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;

internal sealed class CredoWebhookSignatureValidator(IOptions<CredoOptions> options) : ICredoWebhookSignatureValidator
{
    public Task<PaymentProviderWebhookValidationResult> ValidateAsync(
        CredoWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken)
    {
        var secretKey = NormalizeOptional(options.Value.SecretKey);
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, KnownWebhookStatusChangeReasons.Payments.MissingSecretKey));
        }

        var signatureHeader = NormalizeOptional(request.SignatureHeader);
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, KnownWebhookStatusChangeReasons.Shared.MissingSignature));
        }

        var businessCode = NormalizeOptional(request.BusinessCode);
        if (string.IsNullOrWhiteSpace(businessCode))
        {
            return Task.FromResult(new PaymentProviderWebhookValidationResult(SignatureValidationStatus.Invalid, KnownWebhookStatusChangeReasons.Payments.MissingBusinessCode));
        }

        var expectedSignature = ComputeSignature(secretKey, businessCode);
        var validationStatus = VerifyWebhook(signatureHeader, expectedSignature)
            ? SignatureValidationStatus.Valid
            : SignatureValidationStatus.Invalid;
        var statusChangeReason = validationStatus == SignatureValidationStatus.Valid
            ? KnownWebhookStatusChangeReasons.Shared.SignatureVerified
            : KnownWebhookStatusChangeReasons.Shared.InvalidSignature;

        return Task.FromResult(new PaymentProviderWebhookValidationResult(validationStatus, statusChangeReason));
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ComputeSignature(string secretKey, string businessCode)
    {
        using var sha512 = SHA512.Create();
        var payload = Encoding.UTF8.GetBytes(secretKey + businessCode);
        var hash = sha512.ComputeHash(payload);

        return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static bool VerifyWebhook(string signature, string expectedSignature) =>
        expectedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
}
