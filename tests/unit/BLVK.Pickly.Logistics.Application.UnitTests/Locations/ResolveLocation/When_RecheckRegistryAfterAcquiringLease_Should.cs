using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConcurrentResolution_Should
{
[Fact]
    public async Task RecheckRegistryAfterAcquiringLease()
    {
        var context = new LocationResolutionTestContext();
        context.Lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                context.SeedProviderLocation(resolutionKey: LocationResolutionTestContext.ResolutionKey());
                return Guid.CreateVersion7();
            });

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.FromRegistry.ShouldBeTrue();
        await context.ProviderCalls(0);
        await context.Lease.Received(1).ReleaseAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
