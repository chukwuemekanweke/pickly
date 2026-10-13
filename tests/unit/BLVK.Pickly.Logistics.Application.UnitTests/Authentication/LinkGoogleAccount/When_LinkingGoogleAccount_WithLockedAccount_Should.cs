using BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LinkGoogleAccount;

public sealed class When_LinkingGoogleAccount_WithLockedAccount_Should
{
    [Fact]
    public async Task RejectWithoutLinking()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var command = new LinkGoogleAccountCommand(
            "flow-token",
            AuthenticationTestData.StrongPassword(),
            AuthenticationTestData.IpAddress(),
            AuthenticationTestData.UserAgent(),
            new ActorContext(
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Guid.CreateVersion7().ToString("N"),
                Guid.CreateVersion7().ToString("N")));
        var user = AppUser.Create(email);
        user.MarkEmailVerified();
        context.SetGoogleFlow(command.FlowToken, GoogleAuthenticationFlowState.LinkRequired, email, subject);
        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.IsLockedOutAsync(user).Returns(true);

        var result = await context.CreateLinkGoogleAccountHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(LinkGoogleAccountStatus.AccountLocked);
        await context.IdentityService.DidNotReceive().CheckPasswordAsync(user, Arg.Any<string>());
        await context.IdentityService.DidNotReceive().AddLoginAsync(
            Arg.Any<AppUser>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }
}
