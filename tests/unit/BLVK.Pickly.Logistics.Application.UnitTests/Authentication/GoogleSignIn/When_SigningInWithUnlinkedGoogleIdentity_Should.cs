using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignIn;

public sealed class When_SigningInWithUnlinkedGoogleIdentity_Should
{
    [Theory]
    [InlineData(true, GoogleSignInStatus.LinkRequired, GoogleAuthenticationFlowState.LinkRequired)]
    [InlineData(false, GoogleSignInStatus.RegistrationRequired, GoogleAuthenticationFlowState.RegistrationRequired)]
    public async Task ReturnExplicitContinuation(
        bool accountExists,
        GoogleSignInStatus expectedStatus,
        GoogleAuthenticationFlowState expectedFlowState)
    {
        var context = new AuthenticationFlowTestContext();
        const string flowToken = "flow-token";
        const string idToken = "id-token";
        const string nonce = "nonce";
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var command = AuthenticationFlowTestContext.CreateGoogleSignInCommand(idToken) with { FlowToken = flowToken };
        context.SetGoogleFlow(flowToken, GoogleAuthenticationFlowState.Initiated, email, subject);
        context.GoogleIdentityTokenService.ValidateAsync(idToken, nonce, Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, email, "Google User"));
        context.IdentityService.FindByLoginAsync("Google", subject).Returns((AppUser?)null);
        context.IdentityService.FindByEmailAsync(email).Returns(accountExists ? AppUser.Create(email) : null);

        var result = await context.CreateGoogleSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(expectedStatus);
        await context.GoogleAuthenticationFlowService.Received(1).RestoreAsync(
            Arg.Is<GoogleAuthenticationFlow>(flow =>
                flow.State == expectedFlowState &&
                flow.Subject == subject &&
                flow.Email == email),
            Arg.Any<CancellationToken>());
    }
}
