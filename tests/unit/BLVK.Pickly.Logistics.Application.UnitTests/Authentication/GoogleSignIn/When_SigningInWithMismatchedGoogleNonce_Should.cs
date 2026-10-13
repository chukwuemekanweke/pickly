using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignIn;

public sealed class When_SigningInWithMismatchedGoogleNonce_Should
{
    [Fact]
    public async Task RejectCredentialAndRestoreFlow()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignInCommand() with { FlowToken = "flow-token" };
        context.SetGoogleFlow(
            command.FlowToken,
            GoogleAuthenticationFlowState.Initiated,
            AuthenticationTestData.Email(),
            Guid.CreateVersion7().ToString("N"));
        context.GoogleIdentityTokenService.ValidateAsync(
                command.IdToken,
                "nonce",
                Arg.Any<CancellationToken>())
            .Returns((GoogleIdentityTokenPayload?)null);

        var result = await context.CreateGoogleSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignInStatus.InvalidGoogleCredential);
        await context.GoogleAuthenticationFlowService.Received(1).RestoreAsync(
            Arg.Any<GoogleAuthenticationFlow>(),
            Arg.Any<CancellationToken>());
    }
}
