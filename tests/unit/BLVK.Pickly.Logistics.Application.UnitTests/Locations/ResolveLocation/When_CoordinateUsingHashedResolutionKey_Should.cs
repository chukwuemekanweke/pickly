
namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConcurrentResolution_Should
{
[Fact]
    public async Task CoordinateUsingHashedResolutionKey()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderReturns(LocationResolutionTestContext.Candidate());

        await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        await context.Lease.Received(1).TryAcquireAsync(
            LocationResolutionTestContext.ResolutionKey(),
            context.Settings.ResolutionLeaseDuration,
            Arg.Any<CancellationToken>());
    }
}
