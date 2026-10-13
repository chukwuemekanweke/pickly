using BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.PasswordResets;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.PasswordResets;

public sealed class When_CompletingPasswordReset_WithValidOtp_Should
{
    [Fact]
    public async Task ReturnSuccess()
    {
        var context = new AuthenticationControllerTestContext();
        var validator = Substitute.For<IValidator<PasswordResetRequest>>();
        var completeValidator = Substitute.For<IValidator<CompletePasswordResetRequest>>();
        var request = new CompletePasswordResetRequest("jane@example.com", "123456", "P@ssw0rd123!", "P@ssw0rd123!");
        var user = context.CreateUser(request.Email);
        var stakeholder = context.CreateStakeholder(user.Id);

        completeValidator.ValidateAsync(request, Arg.Any<CancellationToken>()).Returns(new ValidationResult());
        context.IdentityService.FindByEmailAsync(request.Email).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(user.Id, request.Otp, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(true);
        context.IdentityService.ResetPasswordAsync(user, request.Password).Returns(IdentityResult.Success);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var sut = new PasswordResetsController(
            context.CreateRequestPasswordResetHandler(),
            context.CreateCompletePasswordResetHandler(),
            validator,
            completeValidator,
            context.CurrentActor);

        var result = await sut.Complete(request, CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        ok.Value.ShouldBeOfType<CompletePasswordResetResponse>();
    }
}
