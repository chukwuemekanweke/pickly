using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetLocation;

public sealed partial class When_GettingLocation_WithLocationId_Should
{
[Fact]
    public async Task HideRetiredLocation()
    {
        var location = ReadModel(LocationResolutionStatus.Retired);
        _repository.GetByIdAsync(location.Id, Arg.Any<CancellationToken>()).Returns(location);

        var result = await new GetLocationHandler(_repository).HandleAsync(
            new GetLocationQuery(location.Id, _actor),
            CancellationToken.None);

        result.Status.ShouldBe(GetLocationStatus.NotFound);
    }
}
