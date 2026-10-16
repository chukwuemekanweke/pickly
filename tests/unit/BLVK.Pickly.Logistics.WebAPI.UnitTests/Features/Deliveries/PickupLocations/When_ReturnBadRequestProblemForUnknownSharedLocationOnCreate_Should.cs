using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task ReturnBadRequestProblemForUnknownSharedLocationOnCreate()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().Create(
            new CreatePickupLocationRequest(Guid.CreateVersion7(), "Home"),
            CancellationToken.None);

        var problem = result.Result.ShouldBeOfType<ObjectResult>();
        problem.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        problem.Value.ShouldBeOfType<ProblemDetails>().Extensions["code"]
            .ShouldBe(PickupLocationErrorCodes.LocationNotFound);
        await context.Repository.DidNotReceive().AddAsync(Arg.Any<SavedPickupLocation>(), Arg.Any<CancellationToken>());
    }
}
