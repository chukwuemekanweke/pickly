using BLVK.Pickly.Logistics.Application.Authentication.Features.RevokeSession;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.RevokeSession;

public sealed class When_RevokingSession_WithOwnedSession_Should
{
    [Fact]
    public async Task PersistRevocation()
    {
        var context = new AuthenticationFlowTestContext();
        var session = AuthenticationSession.Create(Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Browser", null, null, null,
            context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(1));
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        context.SessionService.RevokeAsync(session.Id, session.StakeholderId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await new RevokeSessionHandler(context.SessionService, context.UnitOfWork).HandleAsync(
            new RevokeSessionCommand(session.Id, session.StakeholderId), CancellationToken.None);

        result.ShouldBeTrue();
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
