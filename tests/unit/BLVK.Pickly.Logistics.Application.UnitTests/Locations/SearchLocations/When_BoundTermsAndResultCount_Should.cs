using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
[Fact]
    public async Task BoundTermsAndResultCount()
    {
        LocationSearchRequest? captured = null;
        _repository.SearchAsync(Arg.Do<LocationSearchRequest>(request => captured = request), Arg.Any<CancellationToken>())
            .Returns([]);

        await Handler().HandleAsync(
            new SearchLocationsQuery("a b c d e f g h i j k " + new string('x', 80), null, 500, _actor),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.Terms.Count.ShouldBe(LocationSearchTerms.MaxTerms);
        captured.Limit.ShouldBe(new LocationResolutionOptions().MaxResults);
    }
}
