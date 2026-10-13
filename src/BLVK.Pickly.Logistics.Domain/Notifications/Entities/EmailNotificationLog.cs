using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Localization;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Entities;

public sealed class EmailNotificationLog : Entity, IAggregateRoot
{
    private const int MaxFailureReasonLength = 4000;

    private EmailNotificationLog()
    {
    }

    private EmailNotificationLog(
        Guid messageId,
        Guid tenantId,
        Guid countryId,
        NotificationType notificationType,
        string language,
        Dictionary<string, string> notificationContent,
        string to,
        string? cc,
        string? bcc,
        DateTimeOffset enqueuedAtUtc)
    {
        MessageId = messageId;
        TenantId = tenantId;
        CountryId = countryId;
        NotificationType = notificationType;
        Language = language.Trim().ToLowerInvariant();
        NotificationContent = new Dictionary<string, string>(notificationContent);
        To = to.Trim();
        Cc = string.IsNullOrWhiteSpace(cc) ? null : cc.Trim();
        Bcc = string.IsNullOrWhiteSpace(bcc) ? null : bcc.Trim();
        EnqueuedAtUtc = enqueuedAtUtc;
    }

    public Guid MessageId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CountryId { get; private set; }
    public NotificationType NotificationType { get; private set; }
    public string Language { get; private set; } = SupportedLanguages.Default;
    public Dictionary<string, string> NotificationContent { get; private set; } = [];
    public string To { get; private set; } = string.Empty;
    public string? Cc { get; private set; }
    public string? Bcc { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public DateTimeOffset EnqueuedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    public static EmailNotificationLog Create(
        Guid messageId,
        Guid tenantId,
        Guid countryId,
        NotificationType notificationType,
        Dictionary<string, string> notificationContent,
        string to,
        string? cc,
        string? bcc,
        DateTimeOffset enqueuedAtUtc)
        =>
        new(
            messageId,
            tenantId,
            countryId,
            notificationType,
            SupportedLanguages.Default,
            notificationContent,
            to,
            cc,
            bcc,
            enqueuedAtUtc);

    public static EmailNotificationLog Create(
        Guid messageId,
        Guid tenantId,
        Guid countryId,
        NotificationType notificationType,
        string language,
        Dictionary<string, string> notificationContent,
        string to,
        string? cc,
        string? bcc,
        DateTimeOffset enqueuedAtUtc) =>
        new(messageId, tenantId, countryId, notificationType, language, notificationContent, to, cc, bcc, enqueuedAtUtc);

    public void MarkSent(string providerMessageId, DateTimeOffset utcNow)
    {
        ProviderMessageId = NormalizeProviderMessageId(providerMessageId);
        SentAtUtc = utcNow;
        FailureReason = null;
    }

    public void MarkFailed(string reason)
    {
        SentAtUtc = null;
        FailureReason = NormalizeFailureReason(reason);
    }

    public void MarkDelivered(DateTimeOffset deliveredAtUtc)
    {
        if (DeliveredAtUtc.HasValue)
        {
            return;
        }

        DeliveredAtUtc = deliveredAtUtc;
    }

    private static string NormalizeFailureReason(string reason)
    {
        var normalized = string.IsNullOrWhiteSpace(reason)
            ? "Unknown email delivery failure."
            : reason.Trim();

        return normalized.Length <= MaxFailureReasonLength
            ? normalized
            : normalized[..MaxFailureReasonLength];
    }

    private static string NormalizeProviderMessageId(string providerMessageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerMessageId);

        return providerMessageId.Trim();
    }
}
