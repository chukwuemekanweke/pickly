using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailConfirmations;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.EmailConfirmations.RequestCode;

public sealed class When_RequestingEmailConfirmationCode_WithActiveOtp_Should
{
    [Fact]
    public async Task ReturnExistingRetryTime()
    {
        var context = new AuthenticationControllerTestContext();
        var verifyValidator = Substitute.For<IValidator<SignUpOtpRequest>>();
        var requestValidator = Substitute.For<IValidator<RequestEmailConfirmationOtpRequest>>();
        var request = new RequestEmailConfirmationOtpRequest("jane@example.com");
        var user = context.CreateUser(request.Email);
        var expiresAtUtc = context.Clock.GetUtcNow().AddMinutes(4);
        requestValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(user);
        context.TwoFactorOtpService.GetActiveOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(new TwoFactorOtp("123456", expiresAtUtc));
        var sut = new EmailConfirmationsController(
            context.CreateSignUpOtpHandler(),
            context.CreateRequestEmailConfirmationOtpHandler(),
            verifyValidator,
            requestValidator,
            context.CurrentActor);

        var result = await sut.RequestCode(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var response = ok.Value.ShouldBeOfType<RequestEmailConfirmationOtpResponse>();
        response.RetryAtUtc.ShouldBe(expiresAtUtc);
        await context.CommandSender.DidNotReceiveWithAnyArgs().SendAsync(
            default(SendEmailConfirmationOtpCommand)!,
            default);
    }
}
