using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithProviderFailure_Should
{
[Fact]
    public async Task ReleaseLeaseWhenProviderThrows()
    {
        var context = new LocationResolutionTestContext();
        context.GeocodingProvider.ForwardGeocodeAsync(Arg.Any<GeocodingRequest>(), Arg.Any<CancellationToken>())
            .Returns<GeocodingResult>(_ => throw new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() =>
            context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None));

        await context.Lease.Received(1).ReleaseAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
