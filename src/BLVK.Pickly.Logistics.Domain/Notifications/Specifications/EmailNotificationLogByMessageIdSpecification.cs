using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogByMessageIdSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogByMessageIdSpecification(Guid messageId)
    {
        Where(log => log.MessageId == messageId);
    }
}
