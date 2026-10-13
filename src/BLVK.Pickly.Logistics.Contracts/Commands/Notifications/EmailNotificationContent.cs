namespace BLVK.Pickly.Logistics.Contracts.Commands.Notifications;

public sealed record EmailNotificationContent(
    string To,
    Dictionary<string, string> Content,
    string[]? Cc = null,
    string[]? Bcc = null) : NotificationContent(Content);
