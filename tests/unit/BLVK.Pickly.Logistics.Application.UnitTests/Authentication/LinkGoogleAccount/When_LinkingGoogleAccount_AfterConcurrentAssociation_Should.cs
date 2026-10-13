using BLVK.Pickly.Logistics.Application.Authentication.Features.LinkGoogleAccount;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LinkGoogleAccount;

public sealed class When_LinkingGoogleAccount_AfterConcurrentAssociation_Should
{
    [Fact]
    public async Task TreatSameUserAssociationAsSuccessful()
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
        var stakeholder = Stakeholder.Create(
            user.Id,
            command.ActorContext.TenantId!.Value,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe");
        context.SetGoogleFlow(command.FlowToken, GoogleAuthenticationFlowState.LinkRequired, email, subject);
        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.FindByLoginAsync("Google", subject).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, command.Password).Returns(true);
        context.IdentityService.AddLoginAsync(user, "Google", subject, "Google")
            .Returns(IdentityResult.Failed(new IdentityError
            {
                Code = nameof(IdentityErrorDescriber.LoginAlreadyAssociated)
            }));
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(),
            Arg.Any<CancellationToken>()).Returns(stakeholder);

        var result = await context.CreateLinkGoogleAccountHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(LinkGoogleAccountStatus.Success);
        await context.GoogleAuthenticationFlowService.Received(1).ConsumeAsync(
            Arg.Any<GoogleAuthenticationFlow>(), Arg.Any<CancellationToken>());
    }
}
