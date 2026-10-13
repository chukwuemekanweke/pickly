using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailExistenceChecks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.EmailExistenceChecks;

public sealed class When_CheckingEmailExistence_WithInvalidEmail_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<EmailExistenceCheckRequest>>();
        var request = new EmailExistenceCheckRequest("not-an-email");
        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(
            new ValidationResult([new ValidationFailure(nameof(request.Email), "Invalid email.")]));
        var sut = new EmailExistenceChecksController(context.CreateCheckEmailExistenceHandler(), validator);

        var result = await sut.Handle(request, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        await context.IdentityService.DidNotReceive().FindByEmailAsync(Arg.Any<string>());
    }
}
