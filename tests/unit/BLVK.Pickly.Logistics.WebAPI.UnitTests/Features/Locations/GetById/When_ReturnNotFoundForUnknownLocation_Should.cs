using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.GetById;

public sealed partial class When_GettingLocation_WithLocationId_Should
{
[Fact]
    public async Task ReturnNotFoundForUnknownLocation()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().GetById(Guid.CreateVersion7(), CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }
}
