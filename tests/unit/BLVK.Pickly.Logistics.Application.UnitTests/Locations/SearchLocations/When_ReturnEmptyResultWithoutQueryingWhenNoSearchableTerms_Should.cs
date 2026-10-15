using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
[Fact]
    public async Task ReturnEmptyResultWithoutQueryingWhenNoSearchableTerms()
    {
        var result = await Handler().HandleAsync(new SearchLocationsQuery("  ,.;  ", null, 10, _actor), CancellationToken.None);

        result.Items.ShouldBeEmpty();
        await _repository.DidNotReceive().SearchAsync(Arg.Any<LocationSearchRequest>(), Arg.Any<CancellationToken>());
    }
}
