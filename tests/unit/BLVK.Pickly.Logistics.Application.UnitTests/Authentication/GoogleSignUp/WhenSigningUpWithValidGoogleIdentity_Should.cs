using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignUp;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using StakeholderDefaults = BLVK.Pickly.Logistics.Application.Authentication.Constants.StakeholderDefaults;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningUpWithValidGoogleIdentity_Should
{
    [Fact]
    public async Task ConfirmEmailAndPublishUserCreatedEvent()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var subject = Guid.CreateVersion7().ToString("N");
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var tenantId = Guid.CreateVersion7();
        var stakeholderType = StakeholderType.Create(tenantId, StakeholderDefaults.TypeName, StakeholderDefaults.TypeKey);

        context.SetGoogleFlow("google-id-token", GoogleAuthenticationFlowState.RegistrationRequired, email, subject);
        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>())
            .Returns(IdentityResult.Success);
        context.IdentityService.AddLoginAsync(
                Arg.Any<AppUser>(),
                Arg.Any<string>(),
                subject,
                Arg.Any<string>())
            .Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateGoogleSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateGoogleSignUpCommand(
                idToken: "google-id-token",
                countryId: countryId,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(GoogleSignUpStatus.Success);
        result.Email.ShouldBe(email);
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.EmailConfirmed));
        await context.IdentityService.Received(1).AddLoginAsync(
            Arg.Any<AppUser>(),
            "Google",
            subject,
            "Google");
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message => message.StakeholderId != null),
            Arg.Any<CancellationToken>());
    }
}



