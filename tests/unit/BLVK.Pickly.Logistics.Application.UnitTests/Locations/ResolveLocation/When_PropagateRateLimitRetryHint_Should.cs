using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithProviderFailure_Should
{
[Fact]
    public async Task PropagateRateLimitRetryHint()
    {
        var context = new LocationResolutionTestContext();
        context.ProviderFails(GeocodingStatus.RateLimited, TimeSpan.FromSeconds(42));

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.RetryAfter.ShouldBe(TimeSpan.FromSeconds(42));
    }
}
