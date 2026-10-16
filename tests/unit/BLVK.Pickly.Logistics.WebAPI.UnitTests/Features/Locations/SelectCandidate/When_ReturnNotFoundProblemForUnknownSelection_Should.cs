using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.SelectCandidate;

public sealed partial class When_SelectingLocationCandidate_WithSelectionRequest_Should
{
[Fact]
    public async Task ReturnNotFoundProblemForUnknownSelection()
    {
        var context = new LocationsControllerTestContext();

        var result = await context.CreateController().Register(
            new RegisterLocationRequest($"{LocationSelectionTokens.NewSelectionId()}.0"),
            CancellationToken.None);

        var objectResult = result.Result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
        objectResult.Value.ShouldBeOfType<ProblemDetails>().Extensions["code"].ShouldBe(LocationErrorCodes.SelectionNotFound);
    }
}
