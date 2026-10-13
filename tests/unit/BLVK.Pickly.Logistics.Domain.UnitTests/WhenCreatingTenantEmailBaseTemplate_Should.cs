using BLVK.Pickly.Logistics.Domain.Notifications.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenCreatingTenantEmailBaseTemplate_Should
{
    [Fact]
    public void SetFieldsAndAuditDates()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var tenantId = Guid.CreateVersion7();

        var template = TenantEmailBaseTemplate.Create(tenantId, " Primary tenant brand ", " <html><body>{{:BodyHtml:}}</body></html> ");

        template.TenantId.ShouldBe(tenantId);
        template.Description.ShouldBe("Primary tenant brand");
        template.HtmlTemplate.ShouldBe("<html><body>{{:BodyHtml:}}</body></html>");
        template.CreatedAtUtc.ShouldBe(default);
        template.UpdatedAtUtc.ShouldBe(default);
    }
}


