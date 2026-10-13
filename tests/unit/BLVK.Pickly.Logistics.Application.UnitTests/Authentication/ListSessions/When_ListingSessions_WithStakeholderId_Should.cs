using BLVK.Pickly.Logistics.Application.Authentication.Features.ListSessions;
using BLVK.Pickly.Logistics.Domain.Authentication.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Authentication.ListSessions;

public sealed class When_ListingSessions_WithStakeholderId_Should
{
    [Fact]
    public async Task ScopeLookupToThatStakeholder()
    {
        var context = new AuthenticationFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        context.ActiveSessionReadModelRepository.GetActiveByStakeholderAsync(
                stakeholderId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ActiveSessionReadModel>());

        var result = await context.CreateListSessionsHandler().HandleAsync(
            new ListSessionsQuery(stakeholderId, sessionId), CancellationToken.None);

        result.ShouldBeEmpty();
        await context.ActiveSessionReadModelRepository.Received(1).GetActiveByStakeholderAsync(
            stakeholderId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
}
