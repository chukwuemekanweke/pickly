using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.GoogleSignUp;

public sealed class When_SigningUpWithGoogleIdentity_InMvpMode_Should
{
    [Fact]
    public async Task ResolveMissingProfileDetails()
    {
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateGoogleSignUpCommand() with
        {
            CountryId = null,
            FirstName = null,
            LastName = null,
            Language = null
        };
        Stakeholder? createdStakeholder = null;
        context.RegistrationPolicyProvider.GetAsync().Returns(RegistrationPolicy.Create(isMvp: true));
        context.SetGoogleFlow(
            command.FlowToken,
            GoogleAuthenticationFlowState.RegistrationRequired,
            AuthenticationTestData.Email(),
            "google-subject",
            command.ActorContext.TenantId);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(StakeholderType.Create(command.ActorContext.TenantId!.Value, "Customer", "customer"));
        context.StakeholderRepository.AddAsync(Arg.Do<Stakeholder>(value => createdStakeholder = value))
            .Returns(Task.CompletedTask);

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(command, CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.Success);
        createdStakeholder.ShouldNotBeNull();
        createdStakeholder.FirstName.ShouldStartWith("pikly-");
        createdStakeholder.LastName.ShouldStartWith("pikly-");
        await context.IpGeolocationService.DidNotReceiveWithAnyArgs()
            .GetGeolocationAsync(default!, default);
    }
}
