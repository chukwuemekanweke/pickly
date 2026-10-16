using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.SelectCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionRequest_Should
{
[Fact]
    public async Task ReturnBadRequestForMissingToken()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Register(
            new RegisterLocationRequest(""),
            CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
