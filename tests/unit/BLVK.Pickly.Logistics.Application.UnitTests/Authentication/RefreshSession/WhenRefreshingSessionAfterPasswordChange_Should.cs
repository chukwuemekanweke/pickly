using BLVK.Pickly.Logistics.Application.Authentication.Features.RefreshSession;
using BLVK.Pickly.Logistics.Application.UnitTests.Authentication;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests;

public sealed class WhenRefreshingSessionAfterPasswordChange_Should
{
    [Fact]
    public async Task InvalidateRefreshToken()
    {
        var now = new DateTimeOffset(2026, 4, 4, 0, 0, 0, TimeSpan.Zero);
        var user = AppUser.Create(AuthenticationTestData.Email());
        user.MarkEmailVerified();
        user.SecurityStamp = "current-stamp";

        var stakeholder = Stakeholder.Create(user.Id, Guid.CreateVersion7(), Guid.CreateVersion7(),
            Guid.CreateVersion7(), AuthenticationTestData.FirstName(), AuthenticationTestData.LastName());
        var session = AuthenticationSession.Create(stakeholder.Id,
            Guid.CreateVersion7(), "Unit Test", null, null, null, now, now.AddDays(30));
        var storedRefreshToken = AuthenticationRefreshToken.Create(user.Id, session.Id, "HASH", "previous-stamp", now.AddDays(30));

        var context = new AuthenticationFlowTestContext();
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        context.RefreshTokenService.FindByTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(storedRefreshToken);
        context.IdentityService.FindByIdAsync(user.Id).Returns(user);
        context.IdentityService.GetSecurityStampAsync(user).Returns("current-stamp");
        context.StakeholderRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Stakeholder>>(), Arg.Any<CancellationToken>())
            .Returns(stakeholder);

        var result = await context.CreateRefreshSessionHandler().HandleAsync(
            AuthenticationFlowTestContext.CreateRefreshSessionCommand("refresh-token"),
            CancellationToken.None);

        result.Status.ShouldBe(RefreshSessionStatus.InvalidRefreshToken);
        result.Tokens.ShouldBeNull();
        context.RefreshTokenService.Received(1).Revoke(storedRefreshToken, Arg.Any<DateTimeOffset>());
    }
}







