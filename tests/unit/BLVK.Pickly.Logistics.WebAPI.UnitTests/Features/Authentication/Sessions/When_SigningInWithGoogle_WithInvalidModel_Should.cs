using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_SigningInWithGoogle_WithInvalidModel_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignInRequest>>();
        var googleValidator = Substitute.For<IValidator<GoogleSignInRequest>>();
        var refreshValidator = Substitute.For<IValidator<RefreshSessionRequest>>();
        var request = new GoogleSignInRequest(string.Empty);

        googleValidator.ValidateAsync(request, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure(nameof(GoogleSignInRequest.IdToken), "Required.")]));

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
            context.CreateRevokeOtherSessionsHandler());

        var result = await sut.HandleGoogle(request, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
