using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task ReturnBadRequestProblemForRetiredSharedLocationOnUpdate()
    {
        var context = new PickupLocationsControllerTestContext();
        var location = context.Location();
        context.Repository.FirstOrDefaultAsync(
                Arg.Any<ISpecification<SavedPickupLocation>>(),
                Arg.Any<CancellationToken>())
            .Returns(location);
        context.SharedLocation.Retire();

        var result = await context.CreateController().Update(
            location.Id,
            new UpdatePickupLocationRequest(context.SharedLocation.Id, "Office"),
            CancellationToken.None);

        result.ShouldBeOfType<ObjectResult>().Value.ShouldBeOfType<ProblemDetails>().Extensions["code"]
            .ShouldBe(PickupLocationErrorCodes.LocationUnusable);
    }
}
