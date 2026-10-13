using BLVK.Pickly.Logistics.Application.Authentication.Features.CompletePasswordReset;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenCompletingPasswordResetWithInvalidOtp_Should
{
    [Fact]
    public async Task ReturnInvalidOtp()
    {
        var context = new AuthenticationFlowTestContext();
        var user = context.CreateUser();
        var otp = AuthenticationTestData.Otp();

        context.IdentityService.FindByEmailAsync(user.Email!).Returns(user);
        context.TwoFactorOtpService.ValidateOtpAsync(user.Id, otp, OtpIntent.PasswordReset, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await context.CreateCompletePasswordResetHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateCompletePasswordResetCommand(
                email: user.Email,
                otp: otp),
            CancellationToken.None);

        result.Status.ShouldBe(CompletePasswordResetStatus.InvalidOtp);
        await context.IdentityService.DidNotReceive().ResetPasswordAsync(
            Arg.Any<Domain.Authentication.Entities.AppUser>(),
            Arg.Any<string>());
        context.CustomTelemetryContext.Received().AddCustomEvent(
            Observability.EventNames.Authentication.PasswordResetCompletionFailed,
            Arg.Is<Dictionary<string, string>>(properties =>
                properties[Observability.PropertyNames.Common.FailureReason] == ObservabilityFailureReasons.InvalidOtp));
    }
}

