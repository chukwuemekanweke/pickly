using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithConcurrentResolution_Should
{
[Fact]
    public async Task ReportInProgressWhenLeaseIsNotReleasedInTime()
    {
        var context = new LocationResolutionTestContext();
        context.Lease.TryAcquireAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(ResolveLocationStatus.ResolutionInProgress);
        result.RetryAfter.ShouldNotBeNull();
        await context.ProviderCalls(0);
    }
}
