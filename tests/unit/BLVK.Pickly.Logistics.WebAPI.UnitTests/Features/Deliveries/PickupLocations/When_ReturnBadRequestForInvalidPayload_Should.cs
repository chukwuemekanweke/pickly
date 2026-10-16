using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task ReturnBadRequestForInvalidPayload()
    {
        var context = new PickupLocationsControllerTestContext();
        var request = context.CreateRequest("");

        var result = await context.CreateController().Create(request, CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
        await context.Repository.DidNotReceive().AddAsync(
            Arg.Any<SavedPickupLocation>(),
            Arg.Any<CancellationToken>());
    }
}
