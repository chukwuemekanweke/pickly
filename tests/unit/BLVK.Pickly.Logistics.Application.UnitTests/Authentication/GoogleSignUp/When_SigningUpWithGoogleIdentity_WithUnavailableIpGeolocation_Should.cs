using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Services;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using StakeholderDefaults = BLVK.Pickly.Logistics.Application.Authentication.Constants.StakeholderDefaults;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignUp;

public sealed class When_SigningUpWithGoogleIdentity_WithUnavailableIpGeolocation_Should
{
    [Fact]
    public async Task ContinueRegistration()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignUpCommand();
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var stakeholderType = StakeholderType.Create(
            command.ActorContext.TenantId!.Value,
            StakeholderDefaults.TypeName,
            StakeholderDefaults.TypeKey);
        context.IpGeolocationService.GetGeolocationAsync(command.IpAddress, Arg.Any<CancellationToken>())
            .Returns((IpGeolocation?)null);
        context.SetGoogleFlow(command.FlowToken, GoogleAuthenticationFlowState.RegistrationRequired, email, subject);
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>()).Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(Arg.Any<AppUser>(), "Google", subject, "Google")
            .Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.Success);
        await context.IdentityService.Received(1).CreateAsync(Arg.Any<AppUser>());
    }
}
