using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignUp;

public sealed class WhenSigningUpWithGoogleIdentityAndMismatchedIpCountry_Should
{
    [Fact]
    public async Task RejectRequest()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignUpCommand();
        context.SetGoogleFlow(
            command.FlowToken,
            GoogleAuthenticationFlowState.RegistrationRequired,
            AuthenticationTestData.Email(),
            Guid.CreateVersion7().ToString("N"));
        context.IpGeolocationService.GetGeolocationAsync(command.IpAddress, Arg.Any<CancellationToken>())
            .Returns(new IpGeolocation("Paris", "Ile-de-France", "France"));

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.CountryMismatch);
        await context.GoogleIdentityTokenService.DidNotReceive().ValidateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
