using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.EmailConfirmations;

public sealed class When_VerifyingSignUpOtp_WithAlreadyVerifiedAccount_Should
{
    [Fact]
    public async Task ReturnGenericFailureWithoutTokens()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<SignUpOtpRequest>>();
        var requestCodeValidator = Substitute.For<IValidator<RequestEmailConfirmationOtpRequest>>();
        var request = new SignUpOtpRequest("jane@example.com", "123456");
        var user = context.CreateUser(request.Email);
        user.MarkEmailVerified();

        validator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(user);

        var sut = new EmailConfirmationsController(
            context.CreateSignUpOtpHandler(),
            context.CreateRequestEmailConfirmationOtpHandler(),
            validator,
            requestCodeValidator,
            context.CurrentActor)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await sut.Handle(request, CancellationToken.None);

        var problem = result.Result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        var detail = problem.Value.ShouldBeOfType<ProblemDetails>().Detail;
        detail.ShouldNotBeNull();
        detail!.ShouldNotContain("already verified", Case.Insensitive);
    }
}

