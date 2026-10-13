using BLVK.Pickly.Logistics.Application.Authentication.Features.RevokeOtherSessions;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.RevokeOtherSessions;

public sealed class When_RevokingOtherSessions_WithCurrentSession_Should
{
    [Fact]
    public async Task RevokeOnlyMatchingOwnerScope()
    {
        var context = new AuthenticationFlowTestContext();
        var command = new RevokeOtherSessionsCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        await new RevokeOtherSessionsHandler(context.SessionService, context.UnitOfWork)
            .HandleAsync(command, CancellationToken.None);

        await context.SessionService.Received(1).RevokeOthersAsync(command.CurrentSessionId,
            command.StakeholderId, Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
