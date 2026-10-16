using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionToken_Should
{
[Fact]
    public async Task RejectTokenIssuedToAnotherSender()
    {
        var context = new LocationResolutionTestContext();
        var candidates = await ResolveAmbiguouslyAsync(context);

        var result = await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand(
                candidates[0].SelectionToken,
                context.Actor with { StakeholderId = Guid.CreateVersion7() }),
            CancellationToken.None);

        result.Status.ShouldBe(SelectLocationCandidateStatus.SelectionNotFound);
        context.LocationItems.ShouldBeEmpty();
    }
}
