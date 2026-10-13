using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignUp;

public sealed class When_SigningUpWithNonAuthoritativeGoogleEmail_Should
{
    [Fact]
    public async Task RequireApplicationEmailVerificationWithoutSession()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignUpCommand();
        var email = "person@external.example";
        var subject = Guid.CreateVersion7().ToString("N");
        var stakeholderType = StakeholderType.Create(
            command.ActorContext.TenantId!.Value,
            StakeholderDefaults.TypeName,
            StakeholderDefaults.TypeKey);
        context.SetGoogleFlow(
            command.FlowToken,
            GoogleAuthenticationFlowState.RegistrationRequired,
            email,
            subject,
            hostedDomain: null);
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(Arg.Any<AppUser>(), "Google", subject, "Google")
            .Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
            Arg.Any<ISpecification<StakeholderType>>(),
            Arg.Any<CancellationToken>()).Returns(stakeholderType);

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.EmailVerificationRequired);
        result.Email.ShouldBe(email);
        result.RetryAtUtc.ShouldBe(context.Clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user => user.Email == email && !user.EmailConfirmed));
        await context.SessionService.DidNotReceive().CreateAsync(
            Arg.Any<Stakeholder>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }
}
