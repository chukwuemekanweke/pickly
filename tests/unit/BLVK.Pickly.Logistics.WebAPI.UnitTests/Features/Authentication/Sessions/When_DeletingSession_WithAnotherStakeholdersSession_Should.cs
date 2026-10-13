using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Authentication.Sessions;

public sealed class When_DeletingSession_WithAnotherStakeholdersSession_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new AuthenticationControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var session = AuthenticationSession.Create(Guid.CreateVersion7(),
            Guid.CreateVersion7(), "Browser", null, null, null,
            context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(1));
        context.SessionService.FindAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var controller = context.CreateSessionsController(
            AuthenticationControllerTestContext.CreateSessionPrincipal(Guid.CreateVersion7(), stakeholderId, Guid.CreateVersion7()));

        var result = await controller.DeleteSession(session.Id, CancellationToken.None);

        result.ShouldBeOfType<NotFoundResult>();
        await context.SessionService.DidNotReceive().RevokeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
