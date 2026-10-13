using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Specifications;

public sealed class EmailNotificationLogsByProviderMessageIdsSpecification : Specification<EmailNotificationLog>
{
    public EmailNotificationLogsByProviderMessageIdsSpecification(IReadOnlyCollection<string> providerMessageIds)
    {
        Where(log => log.ProviderMessageId != null && providerMessageIds.Contains(log.ProviderMessageId));
    }
}
