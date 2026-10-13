using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogByProviderMessageIdSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogByProviderMessageIdSpecification(string providerMessageId)
    {
        Where(log => log.ProviderMessageId == providerMessageId);
    }
}
