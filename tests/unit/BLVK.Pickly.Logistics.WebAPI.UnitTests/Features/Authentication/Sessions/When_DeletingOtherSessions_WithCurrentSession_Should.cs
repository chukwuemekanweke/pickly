using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_DeletingOtherSessions_WithCurrentSession_Should
{
    [Fact]
    public async Task RevokeOthersForCurrentStakeholder()
    {
        var context = new AuthenticationControllerTestContext();
        var userId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var controller = context.CreateSessionsController(
            AuthenticationControllerTestContext.CreateSessionPrincipal(userId, stakeholderId, sessionId));

        var result = await controller.DeleteOtherSessions(CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
        await context.SessionService.Received(1).RevokeOthersAsync(sessionId, stakeholderId, Arg.Any<CancellationToken>());
        await context.UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
