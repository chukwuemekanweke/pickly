using BLVK.Pickly.Logistics.Domain.Common.Formatting;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_SigningIn_WithLockedAccount_Should
{
    [Fact]
    public async Task ReturnFormattedProblemDetails()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignInRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignInRequest>>();
        var refreshValidator = Substitute.For<IValidator<RefreshSessionRequest>>();
        var request = new SignInRequest("jane@example.com", "P@ssw0rd123!");
        var user = context.CreateUser(request.Email);
        var stakeholder = context.CreateStakeholder(user.Id);
        var lockedUntilUtc = context.Clock.GetUtcNow().AddHours(2);

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(user);
        context.IdentityService.IsLockedOutAsync(user).Returns(true);
        context.IdentityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

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

        var result = await sut.Handle(request, CancellationToken.None);

        var problem = result.Result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status423Locked);
        var details = problem.Value.ShouldBeOfType<ProblemDetails>();
        details.Detail.ShouldBe(
            $"The account is locked until {DateTimeFormatter.FormatHumanReadableUtc(lockedUntilUtc, context.Clock.GetUtcNow())}.");
    }
}
