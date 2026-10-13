namespace BLVK.Pickly.Logistics.Domain.Notifications.Services;

public interface IMailtrapWebhookSignatureValidator
{
    Task<MailtrapWebhookSignatureValidationResult> ValidateAsync(
        MailtrapWebhookSignatureValidationRequest request,
        CancellationToken cancellationToken);
}
