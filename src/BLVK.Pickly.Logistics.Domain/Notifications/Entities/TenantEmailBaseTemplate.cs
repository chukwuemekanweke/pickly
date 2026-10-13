using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Notifications.Entities;

public sealed class TenantEmailBaseTemplate : Entity
{
    private TenantEmailBaseTemplate()
    {
    }

    private TenantEmailBaseTemplate(
        Guid tenantId,
        string description,
        string htmlTemplate)
    {
        TenantId = tenantId;
        Description = description.Trim();
        HtmlTemplate = htmlTemplate.Trim();
    }

    public Guid TenantId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string HtmlTemplate { get; private set; } = string.Empty;

    public static TenantEmailBaseTemplate Create(
        Guid tenantId,
        string description,
        string htmlTemplate) =>
        new(tenantId, description, htmlTemplate);
}
