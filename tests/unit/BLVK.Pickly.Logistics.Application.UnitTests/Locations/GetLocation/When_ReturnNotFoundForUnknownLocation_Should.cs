using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.GetLocation;

public sealed partial class When_GettingLocation_WithLocationId_Should
{
[Fact]
    public async Task ReturnNotFoundForUnknownLocation()
    {
        var result = await new GetLocationHandler(_repository).HandleAsync(
            new GetLocationQuery(Guid.CreateVersion7(), _actor),
            CancellationToken.None);

        result.Status.ShouldBe(GetLocationStatus.NotFound);
    }
}
