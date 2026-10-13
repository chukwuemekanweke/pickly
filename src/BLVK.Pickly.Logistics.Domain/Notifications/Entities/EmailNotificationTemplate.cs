using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Localization;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Entities;

public sealed class EmailNotificationTemplate : Entity, IAggregateRoot
{
    private EmailNotificationTemplate()
    {
    }

    private EmailNotificationTemplate(
        NotificationType notificationType,
        string language,
        string description,
        string subject,
        string templateFileName)
    {
        NotificationType = notificationType;
        Language = SupportedLanguages.Normalize(language);
        Description = description.Trim();
        Subject = subject.Trim();
        TemplateFileName = templateFileName.Trim();
    }

    public NotificationType NotificationType { get; private set; }
    public string Language { get; private set; } = SupportedLanguages.Default;
    public string Description { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string TemplateFileName { get; private set; } = string.Empty;

    public static EmailNotificationTemplate Create(
        NotificationType notificationType,
        string description,
        string subject,
        string templateFileName) =>
        new(notificationType, SupportedLanguages.Default, description, subject, templateFileName);

    public static EmailNotificationTemplate Create(
        NotificationType notificationType,
        string language,
        string description,
        string subject,
        string templateFileName) =>
        new(notificationType, language, description, subject, templateFileName);
}
