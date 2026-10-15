using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionToken_Should
{
[Fact]
    public async Task NotRecordRequestTextAsIdentityOfChosenCandidate()
    {
        var context = new LocationResolutionTestContext();
        var candidates = await ResolveAmbiguouslyAsync(context);

        await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand(candidates[0].SelectionToken, context.Actor),
            CancellationToken.None);

        context.LocationItems.ShouldHaveSingleItem().ResolutionKeys.ShouldBeEmpty();
    }
}
