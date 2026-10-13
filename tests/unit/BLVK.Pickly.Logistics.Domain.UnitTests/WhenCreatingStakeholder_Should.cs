using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenCreatingStakeholder_Should
{
    [Fact]
    public void SetStakeholderTypeAndAuditFields()
    {
        var appUserId = Guid.CreateVersion7();
        var tenantId = Guid.CreateVersion7();
        var countryId = Guid.CreateVersion7();
        var stakeholderTypeId = Guid.CreateVersion7();
        const string firstName = "Ada";
        const string lastName = "Lovelace";
        var now = new DateTimeOffset(2026, 4, 6, 0, 0, 0, TimeSpan.Zero);

        var stakeholder = Stakeholder.Create(appUserId, tenantId, countryId, stakeholderTypeId, firstName, lastName);

        stakeholder.AppUserId.ShouldBe(appUserId);
        stakeholder.TenantId.ShouldBe(tenantId);
        stakeholder.CountryId.ShouldBe(countryId);
        stakeholder.StakeholderTypeId.ShouldBe(stakeholderTypeId);
        stakeholder.FirstName.ShouldBe(firstName);
        stakeholder.LastName.ShouldBe(lastName);
        stakeholder.Theme.ShouldBe("system");
        stakeholder.Language.ShouldBe("en");
        stakeholder.IsVerified.ShouldBeFalse();
        stakeholder.CreatedAtUtc.ShouldBe(default);
        stakeholder.UpdatedAtUtc.ShouldBe(default);
    }
}


