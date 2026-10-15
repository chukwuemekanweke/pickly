using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SelectLocationCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionToken_Should
{
[Fact]
    public async Task ReturnExistingLocationWhenChosenFeatureIsAlreadyRegistered()
    {
        var context = new LocationResolutionTestContext();
        var candidates = await ResolveAmbiguouslyAsync(context);
        var existing = context.SeedProviderLocation("12 Ekpulechi Close", "address.12b");

        var result = await context.SelectHandler().HandleAsync(
            new SelectLocationCandidateCommand(candidates[1].SelectionToken, context.Actor),
            CancellationToken.None);

        result.Location!.LocationId.ShouldBe(existing.Id);
        context.LocationItems.ShouldHaveSingleItem();
    }
}
