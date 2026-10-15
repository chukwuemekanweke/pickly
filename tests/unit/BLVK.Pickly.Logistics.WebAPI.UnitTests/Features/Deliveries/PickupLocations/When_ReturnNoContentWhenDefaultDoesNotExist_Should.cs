using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task ReturnNoContentWhenDefaultDoesNotExist()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().GetDefault(CancellationToken.None);

        result.Result.ShouldBeOfType<NoContentResult>();
    }
}
