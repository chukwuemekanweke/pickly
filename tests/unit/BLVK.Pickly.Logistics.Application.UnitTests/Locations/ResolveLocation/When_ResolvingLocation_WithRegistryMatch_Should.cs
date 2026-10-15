using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithRegistryMatch_Should
{
    [Fact]
    public async Task ReturnRegisteredLocationByResolutionKeyWithoutCallingProvider()
    {
        var context = new LocationResolutionTestContext();
        var existing = context.SeedProviderLocation(
            "12 Ekpulechi Avenue, Thinkers Corner",
            resolutionKey: LocationResolutionTestContext.ResolutionKey());

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.Resolved);
        result.FromRegistry.ShouldBeTrue();
        result.Location!.LocationId.ShouldBe(existing.Id);
        await context.ProviderCalls(0);
        await context.Lease.DidNotReceive().TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }
}
