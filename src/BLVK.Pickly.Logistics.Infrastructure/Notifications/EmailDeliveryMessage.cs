namespace BLVK.Pickly.Logistics.Infrastructure.Notifications;

internal sealed record EmailDeliveryMessage(
    string FromAddress,
    string FromName,
    string To,
    string Subject,
    string HtmlBody,
    string[]? Cc,
    string[]? Bcc);
