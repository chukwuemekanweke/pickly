using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LoginActivity;

public sealed class When_GettingLoginActivityHistory_WithoutAuthenticatedScope_Should
{
    [Fact]
    public async Task ReturnNotAuthenticatedWithoutQueryingRepository()
    {
        var context = new LoginActivityHistoryTestContext();

        foreach (var actorContext in new[]
        {
            new ActorContext(null, Guid.CreateVersion7(), "correlation-id", "flow-id"),
            new ActorContext(Guid.CreateVersion7(), null, "correlation-id", "flow-id")
        })
        {
            var result = await context.CreateHandler().HandleAsync(
                new GetLoginActivityHistoryCommand(20, null, actorContext),
                CancellationToken.None);

            result.Status.ShouldBe(GetLoginActivityHistoryStatus.NotAuthenticated);
            result.Activities.ShouldBeEmpty();
        }

        await context.Repository.DidNotReceiveWithAnyArgs()
            .GetByStakeholderAsync(default!, default);
    }
}
