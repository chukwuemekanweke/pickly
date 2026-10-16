using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithInvalidRequestContext_Should
{
[Fact]
    public async Task RefuseUnsupportedRegionWithoutCallingProvider()
    {
        var context = new LocationResolutionTestContext();

        var result = await context.ResolveHandler().HandleAsync(context.Command(countryCode: "GH"), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.UnsupportedRegion);
        await context.ProviderCalls(0);
        await context.Lease.DidNotReceive().TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }
}
