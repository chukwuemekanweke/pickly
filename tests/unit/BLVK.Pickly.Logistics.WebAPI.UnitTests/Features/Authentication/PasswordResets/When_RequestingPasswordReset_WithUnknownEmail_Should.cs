using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.PasswordResets;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.PasswordResets;

public sealed class When_RequestingPasswordReset_WithUnknownEmail_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<PasswordResetRequest>>();
        var completeValidator = Substitute.For<IValidator<CompletePasswordResetRequest>>();
        var request = new PasswordResetRequest("missing@example.com");

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns((AppUser?)null);

        var sut = new PasswordResetsController(
            context.CreateRequestPasswordResetHandler(),
            context.CreateCompletePasswordResetHandler(),
            validator,
            completeValidator,
            context.CurrentActor);

        var result = await sut.Handle(request, CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }
}
