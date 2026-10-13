using BLVK.Pickly.Logistics.Domain.Authentication.Entities;

namespace BLVK.Pickly.Logistics.Domain.UnitTests;

public sealed class WhenMarkingEmailVerified_Should
{
    [Fact]
    public void UpdateConfirmation()
    {
        var email = DomainTestData.Email();
        var firstName = DomainTestData.FirstName();
        var lastName = DomainTestData.LastName();

        var createdAt = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var verifiedAt = createdAt.AddMinutes(5);
        var user = AppUser.Create(email);

        user.MarkEmailVerified();

        user.EmailConfirmed.ShouldBeTrue();
    }
}



