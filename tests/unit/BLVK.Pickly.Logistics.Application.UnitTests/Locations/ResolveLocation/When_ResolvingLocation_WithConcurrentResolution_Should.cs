using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConcurrentResolution_Should
{
    [Fact]
    public async Task WaitForConcurrentResolutionInsteadOfCallingProviderAgain()
    {
        var context = new LocationResolutionTestContext();
        var attempts = 0;
        context.Lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                // Another instance holds the lease and finishes registering while this request waits.
                if (Interlocked.Increment(ref attempts) == 2)
                {
                    context.SeedProviderLocation(resolutionKey: LocationResolutionTestContext.ResolutionKey());
                }

                return (Guid?)null;
            });

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        result.FromRegistry.ShouldBeTrue();
        await context.ProviderCalls(0);
    }
}
