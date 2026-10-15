using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionToken_Should
{
[Fact]
    public async Task RejectCandidateIndexOutsideStoredSelection()
    {
        var context = new LocationResolutionTestContext();
        var candidates = await ResolveAmbiguouslyAsync(context);
        var selectionId = candidates[0].SelectionToken.Split('.')[0];

        var result = await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand($"{selectionId}.7", context.Actor),
            CancellationToken.None);

        result.Status.ShouldBe(SelectLocationCandidateStatus.SelectionNotFound);
    }
}
