using BLVK.Pickly.Logistics.Domain.Common.Authentication;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Authentication.Google;

public sealed class When_CheckingGoogleEmailAuthority_Should
{
    [Theory]
    [InlineData("person@gmail.com", null, true)]
    [InlineData("person@company.example", "company.example", true)]
    [InlineData("person@external.example", null, false)]
    public void ApplyGoogleMailboxRules(string email, string? hostedDomain, bool expected)
    {
        var identity = new GoogleIdentityTokenPayload(
            "subject",
            email,
            "Google User",
            EmailVerified: true,
            HostedDomain: hostedDomain);

        GoogleEmailAuthority.IsAuthoritative(identity).ShouldBe(expected);
    }
}
