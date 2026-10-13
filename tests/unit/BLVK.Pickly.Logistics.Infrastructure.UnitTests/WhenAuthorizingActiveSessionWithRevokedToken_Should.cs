using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using NSubstitute;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class WhenAuthorizingActiveSessionWithRevokedToken_Should
{
    [Fact]
    public async Task Fail()
    {
        var tokenId = Guid.CreateVersion7().ToString("N");
        var revocationService = Substitute.For<IAccessTokenRevocationService>();
        revocationService.IsRevokedAsync(tokenId, Arg.Any<CancellationToken>()).Returns(true);
        var sut = new ActiveSessionAuthorizationHandler(revocationService,
            Substitute.For<IAuthenticationSessionService>(), TimeProvider.System);
        var user = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Jti, tokenId)
            ],
            authenticationType: "Bearer"));
        var context = new AuthorizationHandlerContext(
        [
            new ActiveSessionRequirement()
        ],
            user,
            resource: null);

        await sut.HandleAsync(context);

        context.HasSucceeded.ShouldBeFalse();
    }
}

