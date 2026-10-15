using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.SearchLocations;

public sealed partial class When_SearchingLocations_WithQuery_Should
{
[Fact]
    public void TruncateOverlongTerms()
    {
        LocationSearchTerms.Extract(new string('y', 100)).ShouldHaveSingleItem().Length.ShouldBe(LocationSearchTerms.MaxTermLength);
    }
}
