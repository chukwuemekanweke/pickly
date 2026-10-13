namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public sealed record AppPushNotificationContent(
    string Recipient,
    Dictionary<string, string> Content) : NotificationContent(Content);
