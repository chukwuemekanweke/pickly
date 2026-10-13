using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_GettingSessions_WithAuthenticatedUser_Should
{
    [Fact]
    public async Task ReturnOwnedActiveSessions()
    {
        var context = new AuthenticationControllerTestContext();
        var userId = Guid.CreateVersion7();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        context.ActiveSessionReadModelRepository.GetActiveByStakeholderAsync(
                stakeholderId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveSessionReadModel>());
        var controller = context.CreateSessionsController(
            AuthenticationControllerTestContext.CreateSessionPrincipal(userId, stakeholderId, sessionId));

        var result = await controller.GetSessions(CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>();
        await context.ActiveSessionReadModelRepository.Received(1).GetActiveByStakeholderAsync(
            stakeholderId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
