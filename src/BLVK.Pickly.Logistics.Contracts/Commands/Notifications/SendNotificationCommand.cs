namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public sealed record SendNotificationCommand : BaseCommand
{
    public SendNotificationCommand(
        Guid tenantId,
        Guid countryId,
        NotificationType notificationType,
        NotificationMedium notificationMedium,
        NotificationContent notificationContent)
    {
        TenantId = tenantId;
        CountryId = countryId;
        NotificationType = notificationType;
        NotificationMedium = notificationMedium;
        NotificationContent = notificationContent;
    }

    public Guid CountryId { get; }
    public NotificationType NotificationType { get; }
    public NotificationMedium NotificationMedium { get; }
    public NotificationContent NotificationContent { get; }
    public string Language { get; init; } = "en";
}
