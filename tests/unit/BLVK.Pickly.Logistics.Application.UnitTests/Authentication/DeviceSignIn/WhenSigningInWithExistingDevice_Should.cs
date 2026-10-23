using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.DeviceSignIn;

public sealed class WhenSigningInWithExistingDevice_Should
{
    [Fact]
    public async Task ReuseAccountAndReturnTokens()
    {
        const string identifier = "device-123";
        var context = new AuthenticationFlowTestContext();
        var command = AuthenticationFlowTestContext.CreateDeviceSignInCommand(identifier);
        var user = context.CreateUser();
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(
            user.Id,
            command.ActorContext.TenantId!.Value,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Test",
            "User");
        var deviceAccount = DeviceAccount.Create(identifier, user.Id, stakeholder.Id);
        var accessToken = new AccessToken("access-token", context.Clock.GetUtcNow().AddHours(1));
        var refreshToken = new RefreshToken("refresh-token", context.Clock.GetUtcNow().AddDays(30));

        context.DeviceAccountRepository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<DeviceAccount>>(),
                Arg.Any<CancellationToken>())
            .Returns(deviceAccount);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.StakeholderRepository.GetByIdAsync(stakeholder.Id, Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        context.AccessTokenService.Generate(user, stakeholder.Id, Arg.Any<Guid>()).Returns(accessToken);
        context.RefreshTokenService.IssueAsync(
                user,
                Arg.Any<Guid>(),
                Arg.Any<DateTimeOffset>(),
                Arg.Any<CancellationToken>())
            .Returns(refreshToken);

        var result = await context.CreateDeviceSignInHandler().HandleAsync(command, CancellationToken.None);

        result.Tokens.AccessToken.ShouldBe(accessToken);
        result.Tokens.RefreshToken.ShouldBe(refreshToken);
        await context.DeviceAccountLock.Received(1).AcquireAsync(identifier, Arg.Any<CancellationToken>());
        await context.IdentityService.DidNotReceive().CreateAsync(Arg.Any<AppUser>());
        await context.DeviceAccountRepository.DidNotReceive().AddAsync(
            Arg.Any<DeviceAccount>(),
            Arg.Any<CancellationToken>());
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInSuccessful>(message => message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.Transaction.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
