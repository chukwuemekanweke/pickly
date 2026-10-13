using BLVK.Pickly.Logistics.Application.Authentication.Features.CheckEmailExistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailExistenceChecks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.EmailExistenceChecks;

public sealed class When_CheckingEmailExistence_WithExistingEmail_Should
{
    [Fact]
    public async Task ReturnExists()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<EmailExistenceCheckRequest>>();
        var request = new EmailExistenceCheckRequest("jane@example.com");
        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(context.CreateUser(request.Email));
        var sut = new EmailExistenceChecksController(context.CreateCheckEmailExistenceHandler(), validator);

        var result = await sut.Handle(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<CheckEmailExistenceResponse>().Exists.ShouldBeTrue();
    }
}
