namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public sealed record WebPushNotificationContent(
    string Recipient,
    Dictionary<string, string> Content) : NotificationContent(Content);
