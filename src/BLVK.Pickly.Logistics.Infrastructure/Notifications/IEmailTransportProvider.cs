namespace BLVK.Pickly.Logistics.Infrastructure.Notifications;

internal interface IEmailTransportProvider
{
    string ProviderKey { get; }

    Task<EmailTransportSendResult> SendAsync(EmailDeliveryMessage message, CancellationToken cancellationToken = default);
}
