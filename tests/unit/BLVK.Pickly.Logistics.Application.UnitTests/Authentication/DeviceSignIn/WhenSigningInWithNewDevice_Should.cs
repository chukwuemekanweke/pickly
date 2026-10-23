using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.DeviceSignIn;

public sealed class WhenSigningInWithNewDevice_Should
{
    [Fact]
    public async Task CreateVerifiedTestAccountAndReturnTokens()
    {
        const string identifier = "new-device-123";
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateDeviceSignInCommand(identifier);
        var country = Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg");
        var stakeholderType = StakeholderType.Create(
            command.ActorContext.TenantId!.Value,
            "Customer",
            "customer");
        var accessToken = new AccessToken("access-token", context.Clock.GetUtcNow().AddHours(1));
        var refreshToken = new RefreshToken("refresh-token", context.Clock.GetUtcNow().AddDays(30));
        AppUser? createdUser = null;
        Stakeholder? createdStakeholder = null;

        context.DeviceAccountRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<DeviceAccount>>(),
                Arg.Any<CancellationToken>())
            .Returns((DeviceAccount?)null);
        context.IdentityService.CreateAsync(Arg.Do<AppUser>(user => createdUser = user))
            .Returns(Microsoft.AspNetCore.Identity.IdentityResult.Success);
        context.CountryRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Country>>(),
                Arg.Any<CancellationToken>())
            .Returns(country);
        context.StakeholderTypeRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<StakeholderType>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholderType);
        context.StakeholderRepository.AddAsync(
                Arg.Do<Stakeholder>(stakeholder => createdStakeholder = stakeholder),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        context.AccessTokenService.Generate(
                Arg.Any<AppUser>(),
                Arg.Any<Guid>(),
                Arg.Any<Guid>())
            .Returns(accessToken);
        context.RefreshTokenService.IssueAsync(
                Arg.Any<AppUser>(),
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        var result = await context.CreateDeviceSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Tokens.AccessToken.ShouldBe(accessToken);
        result.Tokens.RefreshToken.ShouldBe(refreshToken);
        createdUser.ShouldNotBeNull();
        createdUser.EmailConfirmed.ShouldBeTrue();
        createdUser.Email.ShouldStartWith("device-");
        createdUser.Email.ShouldEndWith("@pickly.test");
        createdStakeholder.ShouldNotBeNull();
        createdStakeholder.TenantId.ShouldBe(command.ActorContext.TenantId.Value);
        createdStakeholder.CountryId.ShouldBe(country.Id);
        createdStakeholder.IsVerified.ShouldBeTrue();
        await context.DeviceAccountRepository.Received(1).AddAsync(
            Arg.Is<DeviceAccount>(account =>
                account.Identifier == identifier &&
                account.AppUserId == createdUser.Id &&
                account.StakeholderId == createdStakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
