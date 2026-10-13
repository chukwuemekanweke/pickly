using BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.ListSessions;

public sealed class When_ListingSessions_WithResolvedLocation_Should
{
    [Fact]
    public async Task IncludeCityStateAndCountry()
    {
        var context = new AuthenticationFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        context.ActiveSessionReadModelRepository.GetActiveByStakeholderAsync(
                stakeholderId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([new ActiveSessionReadModel(sessionId, "Desktop", "Windows", "Chrome",
                "integration-test-agent", "198.51.100.10", "198.51.100.10",
                context.Clock.GetUtcNow(), context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(7),
                "Lagos", "Lagos", "Nigeria")]);

        var result = await context.CreateListSessionsHandler().HandleAsync(
            new ListSessionsQuery(stakeholderId, sessionId), CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].City.ShouldBe("Lagos");
        result[0].State.ShouldBe("Lagos");
        result[0].Country.ShouldBe("Nigeria");
        result[0].IsCurrent.ShouldBeTrue();
    }
}
