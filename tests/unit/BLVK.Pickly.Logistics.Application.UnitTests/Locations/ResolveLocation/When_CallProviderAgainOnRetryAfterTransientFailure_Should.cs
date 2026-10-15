using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithProviderFailure_Should
{
[Fact]
    public async Task CallProviderAgainOnRetryAfterTransientFailure()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderFails(GeocodingStatus.Timeout);
        var handler = context.ResolveHandler();
        await handler.HandleAsync(context.Command(), CancellationToken.None);
        context.ProviderReturns(LocationResolutionTestContext.Candidate());

        var retried = await handler.HandleAsync(context.Command(), CancellationToken.None);

        retried.Status.ShouldBe(ResolveLocationStatus.Resolved);
        await context.ProviderCalls(2);
    }
}
