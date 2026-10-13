using BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_RefreshingSession_WithValidRefreshToken_Should
{
    [Fact]
    public async Task RotateTokens()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignInRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignInRequest>>();
        var refreshValidator = Substitute.For<IValidator<RefreshSessionRequest>>();
        var request = new RefreshSessionRequest("refresh-token");
        var user = context.CreateUser();
        user.MarkEmailVerified();
        user.SecurityStamp = "stamp";
        var stakeholder = context.CreateStakeholder(user.Id);
        var session = AuthenticationSession.Create(stakeholder.Id,
            Guid.CreateVersion7(), "Unit Test", null, null, null, context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(30));
        var storedRefreshToken = AuthenticationRefreshToken.Create(user.Id, session.Id, "HASH", user.SecurityStamp, context.Clock.GetUtcNow().AddDays(30));
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        refreshValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.RefreshTokenService.FindByTokenAsync(request.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns(user.SecurityStamp);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id, session.Id)
            .Returns(new AccessToken("new-access-token", context.Clock.GetUtcNow().AddMinutes(15)));
        context.RefreshTokenService.RotateAsync(storedRefreshToken, user, Arg.Any<CancellationToken>())
            .Returns(new RefreshToken("new-refresh-token", context.Clock.GetUtcNow().AddDays(7)));

        var sut = new SessionsController(
            context.CreateSignInHandler(),
            context.CreateGoogleSignInHandler(),
            context.CreateLogoutSessionHandler(),
            context.CreateRefreshSessionHandler(),
            validator,
            googleValidator,
            refreshValidator,
            context.Clock,
            context.CurrentActor,
            context.CreateListSessionsHandler(),
            context.CreateRevokeSessionHandler(),
            context.CreateRevokeOtherSessionsHandler())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        sut.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        sut.Request.Headers.UserAgent = "Unit Test";

        var result = await sut.Refresh(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<RefreshSessionResponse>();
        payload.AccessToken.ShouldBe("new-access-token");
        payload.RefreshToken.ShouldBe("new-refresh-token");
    }
}


