using BLVK.Pickly.Logistics.Application.Authentication.Features.SignUp;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Microsoft.AspNetCore.Identity;
using Shouldly;
using StakeholderDefaults = BLVK.Pickly.Logistics.Application.Authentication.Constants.StakeholderDefaults;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningUpWithNewEmail_Should
{
    [Fact]
    public async Task CreateIdentityUserAndPublishUserCreatedEvent()
    {
        var context = new AuthenticationFlowTestContext();
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var countryId = Guid.CreateVersion7();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var tenantId = Guid.CreateVersion7();
        var stakeholderType = StakeholderType.Create(tenantId, StakeholderDefaults.TypeName, StakeholderDefaults.TypeKey);

        context.IdentityService.FindByEmailAsync(email).Returns((AppUser?)null);
        context.IdentityService.CreateAsync(Arg.Any<AppUser>(), Arg.Any<string>()).Returns(IdentityResult.Success);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);

        var result = await context.CreateSignUpHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignUpCommand(
                email: email,
                password: password,
                countryId: countryId,
                firstName: firstName,
                lastName: lastName),
            CancellationToken.None);

        result.Status.ShouldBe(SignUpStatus.Accepted);
        result.RetryAtUtc.ShouldBe(context.Clock.GetUtcNow().Add(AuthenticationOtpDefaults.EmailConfirmationLifetime));
        await context.IdentityService.Received(1).CreateAsync(
            Arg.Is<AppUser>(user =>
                user.Email == email &&
                user.UserName == email &&
                user.EmailConfirmed == false),
            password);
        await context.StakeholderRepository.Received(1).AddAsync(
            Arg.Is<Domain.Stakeholders.Entities.Stakeholder>(stakeholder =>
                stakeholder.CountryId == countryId &&
                stakeholder.FirstName == firstName &&
                stakeholder.LastName == lastName),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserCreated>(message =>
                message.StakeholderId != null &&
                message.RequestedAtUtc == context.Clock.GetUtcNow() &&
                message.ExpiresAtUtc == result.RetryAtUtc),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}



