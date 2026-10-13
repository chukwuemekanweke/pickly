using BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LinkGoogleAccount;

public sealed class When_LinkingGoogleAccount_FromAnotherTenant_Should
{
    [Fact]
    public async Task RejectTheFlowWithoutLinkingTheAccount()
    {
        var context = new AuthenticationFlowTestContext();
        var tenantId = Guid.CreateVersion7();
        var command = new LinkGoogleAccountCommand(
            "flow-token",
            AuthenticationTestData.StrongPassword(),
            AuthenticationTestData.IpAddress(),
            AuthenticationTestData.UserAgent(),
            new ActorContext(
                Guid.CreateVersion7(),
                tenantId,
                Guid.CreateVersion7().ToString("N"),
                Guid.CreateVersion7().ToString("N")));
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
            GoogleAuthenticationFlowState.LinkRequired,
            email,
            subject,
            tenantId);
        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, command.Password).Returns(true);
        context.StakeholderRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateLinkGoogleAccountHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(LinkGoogleAccountStatus.GoogleFlowInvalid);
        await context.IdentityService.DidNotReceive().AddLoginAsync(
            Arg.Any<AppUser>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>());
        await context.GoogleAuthenticationFlowService.Received(1).ConsumeAsync(
            Arg.Is<GoogleAuthenticationFlow>(flow => flow.Token == command.FlowToken),
            Arg.Any<CancellationToken>());
    }
}
