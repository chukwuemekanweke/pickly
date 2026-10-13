using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignIn;

public sealed class When_SigningInWithGoogleIdentityFromAnotherTenant_Should
{
    [Fact]
    public async Task RejectTheFlowWithoutCreatingASession()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignInCommand();
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var user = AppUser.Create(email);
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe");
        context.SetGoogleFlow(
            command.FlowToken,
            GoogleAuthenticationFlowState.Initiated,
            email,
            subject,
            command.ActorContext.TenantId);
        context.GoogleIdentityTokenService.ValidateAsync(command.IdToken, "nonce", Arg.Any<CancellationToken>())
            .Returns(new GoogleIdentityTokenPayload(subject, email, "Google User"));
        context.IdentityService.FindByLoginAsync("Google", subject).Returns(user);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateGoogleSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignInStatus.GoogleFlowInvalid);
        await context.SessionService.DidNotReceive().CreateAsync(
            Arg.Any<Stakeholder>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
        await context.GoogleAuthenticationFlowService.Received(1).ConsumeAsync(
            Arg.Is<GoogleAuthenticationFlow>(flow => flow.Token == command.FlowToken),
            Arg.Any<CancellationToken>());
    }
}
