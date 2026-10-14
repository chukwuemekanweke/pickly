using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Localization;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.SignUp;

public sealed class When_SigningUp_InMvpMode_Should
{
    [Fact]
    public async Task ResolveMissingProfileDetails()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateSignUpCommand() with
        {
            CountryId = null,
            FirstName = null,
            LastName = null,
            Language = null
        };
        Stakeholder? createdStakeholder = null;
        context.RegistrationPolicyProvider.GetAsync().Returns(RegistrationPolicy.Create(isMvp: true));
        context.IdentityService.FindByEmailAsync(command.Email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), command.Password).Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(StakeholderType.Create(command.ActorContext.TenantId!.Value, "Customer", "customer"));
        context.StakeholderRepository.AddAsync(Arg.Do<Stakeholder>(value => createdStakeholder = value))
            .Returns(Task.CompletedTask);

        var result = await context.CreateSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        createdStakeholder.ShouldNotBeNull();
        createdStakeholder.FirstName.ShouldStartWith("pikly-");
        createdStakeholder.LastName.ShouldStartWith("pikly-");
        createdStakeholder.FirstName.ShouldNotBe(createdStakeholder.LastName);
        createdStakeholder.Language.ShouldBe(SupportedLanguages.Default);
        await context.IpGeolocationService.DidNotReceiveWithAnyArgs()
            .GetGeolocationAsync(default!, default);
    }
}
