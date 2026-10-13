using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.SignIn;

public sealed class When_SigningInWithWrongPassword_Should
{
    [Fact]
    public async Task RecordFailedAccessAttempt()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateSignInCommand(password: "wrong-password");
        var user = AppUser.Create(command.Email);
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(
            user.Id,
            command.ActorContext.TenantId!.Value,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Jane",
            "Doe");
        context.IdentityService.FindByEmailAsync(command.Email).Returns(user);
        context.IdentityService.CheckPasswordAsync(user, command.Password).Returns(false);
        context.StakeholderRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<Stakeholder>>(),
            Arg.Any<CancellationToken>()).Returns(stakeholder);

        var result = await context.CreateSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.InvalidCredentials);
        await context.IdentityService.Received(1).AccessFailedAsync(user);
        await context.IdentityService.DidNotReceive().ResetAccessFailedCountAsync(user);
    }
}
