using BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.ListSessions;

public sealed class When_ListingSessions_WithUnresolvedLocation_Should
{
    [Fact]
    public async Task ReturnNullCityStateAndCountryWithoutFailing()
    {
        var context = new AuthenticationFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        context.ActiveSessionReadModelRepository.GetActiveByStakeholderAsync(
                stakeholderId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns([new ActiveSessionReadModel(sessionId, "Desktop", "Windows", "Chrome",
                "integration-test-agent", "127.0.0.1", "127.0.0.1",
                context.Clock.GetUtcNow(), context.Clock.GetUtcNow(), context.Clock.GetUtcNow().AddDays(7),
                null, null, null)]);

        var result = await context.CreateListSessionsHandler().HandleAsync(
            new ListSessionsQuery(stakeholderId, sessionId), CancellationToken.None);

        result.Count.ShouldBe(1);
        result[0].City.ShouldBeNull();
        result[0].State.ShouldBeNull();
        result[0].Country.ShouldBeNull();
        result[0].LastIpAddress.ShouldBe("127.0.0.1");
    }
}
