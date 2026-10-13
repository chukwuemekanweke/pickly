using BLVK.Pickly.Logistics.Application.Authentication.Features.RequestEmailConfirmationOtp;
using BLVK.Pickly.Logistics.Contracts.Commands.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.RequestEmailConfirmationOtp;

public sealed class When_RequestingEmailConfirmationOtp_WithActiveOtp_Should
{
    [Fact]
    public async Task ReturnExistingRetryTimeWithoutQueuingAnotherCode()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var user = context.CreateUser(email);
        var expiresAtUtc = context.Clock.GetUtcNow().AddMinutes(7);
        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.TwoFactorOtpService.GetActiveOtpAsync(
                user.Id,
                OtpIntent.EmailConfirmation,
                Arg.Any<CancellationToken>())
            .Returns(new TwoFactorOtp(AuthenticationTestData.Otp(), expiresAtUtc));

        var result = await context.CreateRequestEmailConfirmationOtpHandler().HandleAsync(
            new RequestEmailConfirmationOtpCommand(
                email,
                AuthenticationFlowTestContext.CreateSignUpCommand(email: email).ActorContext),
            CancellationToken.None);

        result.Status.ShouldBe(RequestEmailConfirmationOtpStatus.Accepted);
        result.RetryAtUtc.ShouldBe(expiresAtUtc);
        await context.CommandSender.DidNotReceiveWithAnyArgs().SendAsync(
            default(SendEmailConfirmationOtpCommand)!,
            default);
        await context.UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}
