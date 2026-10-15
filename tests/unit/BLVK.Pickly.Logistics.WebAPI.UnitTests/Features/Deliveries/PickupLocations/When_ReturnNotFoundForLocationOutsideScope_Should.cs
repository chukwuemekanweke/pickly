using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task ReturnNotFoundForLocationOutsideScope()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().GetById(Guid.CreateVersion7(), CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundResult>();
    }
}
