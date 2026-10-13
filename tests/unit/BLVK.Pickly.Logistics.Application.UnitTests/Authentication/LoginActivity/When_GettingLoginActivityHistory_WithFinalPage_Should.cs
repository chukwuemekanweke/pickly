using BLVK.Pickly.Logistics.Application.Authentication.Features.GetLoginActivityHistory;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.LoginActivity;

public sealed class When_GettingLoginActivityHistory_WithFinalPage_Should
{
    [Fact]
    public async Task ReturnNullNextCursor()
    {
        var context = new LoginActivityHistoryTestContext();
        context.Repository.GetByStakeholderAsync(
                Arg.Any<LoginActivityHistoryCursorRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new LoginActivityHistoryCursorPage(
                [new LoginActivityHistoryReadModel(
                    Guid.CreateVersion7(), LoginActivityType.InitialLogin, DateTimeOffset.UtcNow,
                    "203.0.113.10", null, null, null, "Lagos", "Lagos", "Nigeria")],
                false));

        var result = await context.CreateHandler().HandleAsync(
            new GetLoginActivityHistoryCommand(
                20,
                null,
                new ActorContext(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        result.NextCursor.ShouldBeNull();
        result.Activities[0].Country.ShouldBe("Nigeria");
    }
}
