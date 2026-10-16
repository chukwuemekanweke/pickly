using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.PickupLocations;

public sealed partial class When_HandlingPickupLocationRequests_Should
{
[Fact]
    public async Task RejectMissingLocationIdAndOverlongDirections()
    {
        var context = new PickupLocationsControllerTestContext();

        var result = await context.CreateController().Create(
            new CreatePickupLocationRequest(Guid.Empty, "Home", new string('x', SavedPickupLocation.MaxAdditionalDirectionsLength + 1)),
            CancellationToken.None);

        var badRequest = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        var errors = badRequest.Value.ShouldBeOfType<ValidationProblemDetails>().Errors;
        errors.Keys.ShouldContain(nameof(CreatePickupLocationRequest.LocationId));
        errors.Keys.ShouldContain(nameof(CreatePickupLocationRequest.AdditionalDirections));
    }
}
