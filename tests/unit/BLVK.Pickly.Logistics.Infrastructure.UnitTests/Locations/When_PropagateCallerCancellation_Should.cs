using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Fact]
    public async Task PropagateCallerCancellation()
    {
        var context = new MapboxGeocodingTestContext
        {
            Responder = async (_, cancellationToken) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                return MapboxGeocodingTestContext.Json(MapboxGeocodingTestContext.AddressFeatureCollection);
            }
        };
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<OperationCanceledException>(() => context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), cancellation.Token));
    }
}
