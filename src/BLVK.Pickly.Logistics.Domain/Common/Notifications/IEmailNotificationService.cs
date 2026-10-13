using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

namespace BLVK.Pickly.Logistics.Domain.Common.Notifications;

public interface IEmailNotificationService
{
    Task<EmailNotificationSendResult?> SendAsync(SendNotificationCommand command, CancellationToken cancellationToken = default);
}
