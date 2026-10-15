using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed partial class When_GeocodingWithMapbox_WithProviderFailure_Should
{
[Theory]
    [InlineData("PermanentStorageEnabled", "false")]
    [InlineData("AccessToken", "")]
    public async Task RefuseWithoutCallingMapboxWhenPermanentStorageIsNotEnabled(string key, string value)
    {
        var context = new MapboxGeocodingTestContext().Configure(key, value);

        var result = await context.CreateProvider()
            .ForwardGeocodePermanentAsync(MapboxGeocodingTestContext.Request(), CancellationToken.None);

        result.Status.ShouldBe(GeocodingStatus.PermanentStorageUnavailable);
        context.Requests.ShouldBeEmpty();
    }
}
