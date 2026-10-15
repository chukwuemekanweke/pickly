using BLVK.Pickly.Logistics.WebAPI.Features.Locations;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Locations.Resolve;

public sealed partial class When_ResolvingLocation_WithResolveRequest_Should
{
[Fact]
    public async Task ReturnConflictWithRetryAfterWhenResolutionIsInProgress()
    {
        var context = new LocationsControllerTestContext();
        context.Lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);
        var controller = context.CreateController();

        var result = await controller.Resolve(LocationsControllerTestContext.ResolveRequest(), CancellationToken.None);

        AssertProblem(result, StatusCodes.Status409Conflict, LocationErrorCodes.ResolutionInProgress);
        controller.Response.Headers.RetryAfter.ToString().ShouldBe("2");
    }
}
