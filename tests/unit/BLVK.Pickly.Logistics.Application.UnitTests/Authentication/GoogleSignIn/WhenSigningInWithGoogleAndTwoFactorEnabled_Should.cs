using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningInWithGoogleAndTwoFactorEnabled_Should
{
    [Fact]
    public async Task ConsumeGoogleFlowAndReturnChallenge()
    {
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Jane", "Doe");
        var context = new AuthenticationFlowTestContext();
        const string flowToken = "google-flow";
        const string subject = "google-subject";
        var command = AuthenticationFlowTestContext.CreateGoogleSignInCommand() with { FlowToken = flowToken };
        context.SetGoogleFlow(flowToken, GoogleAuthenticationFlowState.Initiated, user.Email!, subject);
        context.GoogleIdentityTokenService.ValidateAsync(command.IdToken, "nonce", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, user.Email!, "Jane Doe"));
        context.IdentityService.FindByLoginAsync("Google", subject).Returns(user);
        context.IdentityService.GetTwoFactorEnabledAsync(user).Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>()).Returns(stakeholder);
        var challenge = new TwoFactorChallenge("opaque", user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Google, command.ActorContext, command.IpAddress, command.UserAgent,
            context.Clock.GetUtcNow().AddMinutes(5), 5);
        context.TwoFactorChallengeService.StartAsync(user.Id, stakeholder.Id, stakeholder.TenantId,
            AuthenticationMethod.Google, command.ActorContext, command.IpAddress, command.UserAgent,
            Arg.Any<CancellationToken>()).Returns(challenge);

        var result = await context.CreateGoogleSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignInStatus.RequiresTwoFactor);
        result.Tokens.ShouldBeNull();
        await context.GoogleAuthenticationFlowService.Received(1).ConsumeAsync(
            Arg.Is<GoogleAuthenticationFlow>(flow => flow.Token == flowToken), Arg.Any<CancellationToken>());
        await context.SessionService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default!, default!, default, default);
    }
}
