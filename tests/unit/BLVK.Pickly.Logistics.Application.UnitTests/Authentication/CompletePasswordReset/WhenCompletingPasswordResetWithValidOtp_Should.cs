using BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenCompletingPasswordResetWithValidOtp_Should
{
    [Fact]
    public async Task ResetPassword()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var otp = AuthenticationTestData.Otp();
        var password = AuthenticationTestData.StrongPassword();

        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(user.Id, otp, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(true);
        context.IdentityService.ResetPasswordAsync(user, password).Returns(IdentityResult.Success);
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            AuthenticationTestData.FirstName(),
            AuthenticationTestData.LastName());
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateCompletePasswordResetHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateCompletePasswordResetCommand(
                email: user.Email,
                otp: otp,
                password: password,
                confirmPassword: password),
            CancellationToken.None);

        result.Status.ShouldBe(CompletePasswordResetStatus.Success);
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.SessionService.Received(1).RevokeAllAsync(stakeholder.Id, Arg.Any<CancellationToken>());
    }
}



