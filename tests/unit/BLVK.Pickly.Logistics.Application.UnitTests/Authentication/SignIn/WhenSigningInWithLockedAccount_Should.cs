using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Contracts.Events;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenSigningInWithLockedAccount_Should
{
    [Fact]
    public async Task ReturnAccountLockedAndPublishFailedEvent()
    {
        var email = AuthenticationTestData.Email();
        var password = AuthenticationTestData.StrongPassword();
        var firstName = AuthenticationTestData.FirstName();
        var lastName = AuthenticationTestData.LastName();
        var lockedUntilUtc = new DateTimeOffset(2026, 4, 7, 12, 0, 0, TimeSpan.Zero);
        var ipAddress = AuthenticationTestData.IpAddress();
        var userAgent = AuthenticationTestData.UserAgent();
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(email);
        user.MarkEmailVerified();
        var stakeholder = Stakeholder.Create(
            user.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            firstName,
            lastName);

        var context = new AuthenticationFlowTestContext();
        context.IdentityService.FindByEmailAsync(email).Returns(user);
        context.IdentityService.IsLockedOutAsync(user).Returns(true);
        context.IdentityService.GetLockoutEndUtcAsync(user).Returns(lockedUntilUtc);
        context.StakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateSignInHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateSignInCommand(email, password, ipAddress, userAgent),
            CancellationToken.None);

        result.Status.ShouldBe(SignInStatus.AccountLocked);
        result.Tokens.ShouldBeNull();
        result.LockedUntilUtc.ShouldBe(lockedUntilUtc);
        await context.EventPublisher.Received(1).PublishAsync(
            Arg.Is<UserSignInFailed>(message =>
                message.EmailAddress == email &&
                message.IpAddress == ipAddress &&
                message.UserAgent == userAgent &&
                message.FailureReason == UserSignInFailureReasons.LockedOut &&
                message.StakeholderId == stakeholder.Id),
            Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await context.IdentityService.DidNotReceive().CheckPasswordAsync(Arg.Any<AppUser>(), Arg.Any<string>());
    }
}








