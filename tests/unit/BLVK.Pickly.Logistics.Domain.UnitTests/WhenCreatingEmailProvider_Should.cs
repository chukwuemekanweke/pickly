using BLVK.Pickly.Logistics.Domain.Providers.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenCreatingEmailProvider_Should
{
    [Fact]
    public void SetProviderNameProviderKeyIsActiveAndAuditFields()
    {
        var utcNow = DateTimeOffset.UtcNow;

        var provider = Provider.Create(ProviderType.Email, " Mailtrap ", " mailtrap ", true);

        provider.ProviderName.ShouldBe("Mailtrap");
        provider.ProviderKey.ShouldBe("mailtrap");
        provider.IsActive.ShouldBeTrue();
        provider.CreatedAtUtc.ShouldBe(default);
        provider.UpdatedAtUtc.ShouldBe(default);
    }
}



