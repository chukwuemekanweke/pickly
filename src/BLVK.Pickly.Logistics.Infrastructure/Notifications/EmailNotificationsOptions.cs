namespace BLVK.Pickly.Logistics.Infrastructure.Notifications;

public sealed class EmailNotificationsOptions
{
    public const string SectionName = "Notifications:Email";

    public string FromAddress { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public string TemplateSetsRootPath { get; init; } = "EmailTemplates/TemplateSets";
    public string DefaultBrandKey { get; init; } = "default";
    public string BaseTemplateFileName { get; init; } = "BaseTemplate.html";
    public string NotificationTemplatesFolder { get; init; } = "NotificationTypes";
    public MailtrapOptions Mailtrap { get; init; } = new();

    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(FromAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(FromName);
        ArgumentException.ThrowIfNullOrWhiteSpace(TemplateSetsRootPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(DefaultBrandKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(BaseTemplateFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(NotificationTemplatesFolder);
    }

    public sealed class MailtrapOptions
    {
        public string ApiToken { get; init; } = string.Empty;
        public bool UseBulkApi { get; init; }
        public long? InboxId { get; init; }
        public string WebhookSigningSecret { get; init; } = string.Empty;

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(ApiToken);
        }
    }
}
