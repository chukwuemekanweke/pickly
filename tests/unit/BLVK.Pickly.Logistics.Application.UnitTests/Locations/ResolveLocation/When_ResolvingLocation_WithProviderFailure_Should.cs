using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Locations.ResolveLocation;

public sealed partial class When_ResolvingLocation_WithProviderFailure_Should
{
    [Theory]
    [InlineData(GeocodingStatus.Timeout, ResolveLocationStatus.ProviderUnavailable)]
    [InlineData(GeocodingStatus.ProviderUnavailable, ResolveLocationStatus.ProviderUnavailable)]
    [InlineData(GeocodingStatus.RateLimited, ResolveLocationStatus.ProviderRateLimited)]
    [InlineData(GeocodingStatus.PermanentStorageUnavailable, ResolveLocationStatus.PermanentGeocodingUnavailable)]
    [InlineData(GeocodingStatus.InvalidResponse, ResolveLocationStatus.InvalidProviderResponse)]
    public async Task MapFailureWithoutPersistingOrCaching(GeocodingStatus providerStatus, ResolveLocationStatus expected)
    {
        var context = new LocationResolutionTestContext();
        context.ProviderFails(providerStatus);

        var result = await context.ResolveHandler().HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(expected);
        result.Location.ShouldBeNull();
        context.LocationItems.ShouldBeEmpty();
        context.CacheKeys.ShouldBeEmpty();
    }
}
