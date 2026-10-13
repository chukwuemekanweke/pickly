using BLVK.Pickly.Logistics.Domain.Common;
using BLVK.Pickly.Logistics.Domain.Notifications.Services;
using BLVK.Pickly.Logistics.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Security.Cryptography;
using System.Text;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_ValidatingMailtrapWebhook_WithValidSignature_Should
{
    [Fact]
    public async Task ReturnValidStatus()
    {
        const string rawPayload = "{\"events\":[{\"event\":\"delivery\"}]}";
        var sut = new MailtrapWebhookSignatureValidator(
            Options.Create(new EmailNotificationsOptions
            {
                Mailtrap = new EmailNotificationsOptions.MailtrapOptions
                {
                    WebhookSigningSecret = "test-signing-secret"
                }
            }));
        var signature = ComputeSignature("test-signing-secret", rawPayload);

        var result = await sut.ValidateAsync(
            new MailtrapWebhookSignatureValidationRequest(signature, rawPayload),
            CancellationToken.None);

        result.IsValid.ShouldBeTrue();
        result.StatusChangeReason.ShouldBe(KnownWebhookStatusChangeReasons.Shared.SignatureVerified);
    }

    private static string ComputeSignature(string signingSecret, string rawPayload)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret), Encoding.UTF8.GetBytes(rawPayload));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
