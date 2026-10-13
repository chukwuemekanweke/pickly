using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenCreatingAppUser_Should
{
    [Fact]
    public void SetTrimmedNamesAndAuditFields()
    {
        const string rawEmail = "  ada@example.com  ";
        const string expectedEmail = "ada@example.com";

        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);

        var user = AppUser.Create(rawEmail);

        user.Email.ShouldBe(expectedEmail);
        user.UserName.ShouldBe(expectedEmail);
        user.CreatedAtUtc.ShouldBe(default);
        user.UpdatedAtUtc.ShouldBe(default);
    }
}


