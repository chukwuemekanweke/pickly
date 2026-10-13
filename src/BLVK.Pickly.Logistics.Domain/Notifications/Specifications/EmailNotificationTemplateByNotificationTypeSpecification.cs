using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class EmailNotificationTemplateByNotificationTypeSpecification : Specification<EmailNotificationTemplate>
{
    public EmailNotificationTemplateByNotificationTypeSpecification(
        NotificationType notificationType,
        string language = "en")
    {
        Where(template => template.NotificationType == notificationType && template.Language == language);
    }
}
