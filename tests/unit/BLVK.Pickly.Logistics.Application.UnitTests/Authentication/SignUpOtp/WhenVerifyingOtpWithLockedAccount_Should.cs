using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUpOtp;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenVerifyingOtpWithLockedAccount_Should
{
    [Fact]
    public async Task RejectSessionIssuance()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var user = context.CreateUser(email);

        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.IsLockedOutAsync(user).Returns(true);

        var result = await context.CreateSignUpOtpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpOtpCommand(email),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpOtpStatus.AccountUnavailable);
        result.Tokens.ShouldBeNull();
        await context.TwoFactorOtpService.DidNotReceiveWithAnyArgs().ValidateOtpAsync(
            default,
            string.Empty,
            default,
            CancellationToken.None);
        context.AccessTokenService.DidNotReceiveWithAnyArgs().Generate(default!, default, default);
        await context.RefreshTokenService.DidNotReceiveWithAnyArgs().IssueAsync(
            default!,
            default(Guid),
            default(DateTimeOffset),
            CancellationToken.None);
    }
}
