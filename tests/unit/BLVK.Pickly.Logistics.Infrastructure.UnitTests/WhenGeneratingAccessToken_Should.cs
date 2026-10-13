using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenGeneratingAccessToken_Should
{
    [Fact]
    public void IncludeStakeholderIdClaim()
    {
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var user = AppUser.Create(InfrastructureTestData.Email());
        var sut = new JwtTokenGenerator(
            Options.Create(new JwtOptions
            {
                Issuer = "tests",
                Audience = "tests"
            }),
            new FakeTimeProvider());

        var token = sut.Generate(user, stakeholderId, sessionId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.Value);

        jwt.Claims.Single(claim => claim.Type == CustomClaimTypes.StakeholderId).Value.ShouldBe(stakeholderId.ToString());
        jwt.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value.ShouldBe(sessionId.ToString());
    }

    private sealed class FakeTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 4, 16, 0, 0, 0, TimeSpan.Zero);
    }
}





