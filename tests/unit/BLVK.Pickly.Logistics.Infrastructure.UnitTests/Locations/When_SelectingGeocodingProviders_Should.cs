using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Infrastructure.Locations;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Locations;

public sealed class When_SelectingGeocodingProviders_Should
{
    [Fact]
    public async Task RoundRobinInConfiguredOrderAndPreserveActualIdentity()
    {
        var geoapify = new FakeProvider("geoapify", LocationResolutionSource.Geoapify);
        var locationIq = new FakeProvider("locationiq", LocationResolutionSource.LocationIq);
        var selector = CreateSelector(geoapify, locationIq);

        var results = new List<GeocodingResult>();
        for (var index = 0; index < 5; index++)
        {
            results.Add(await selector.ForwardGeocodeAsync(Request(), CancellationToken.None));
        }

        results.Select(result => result.Provider).ShouldBe(["geoapify", "locationiq", "geoapify", "locationiq", "geoapify"]);
        results.Select(result => result.ResolutionSource).ShouldBe([
            LocationResolutionSource.Geoapify,
            LocationResolutionSource.LocationIq,
            LocationResolutionSource.Geoapify,
            LocationResolutionSource.LocationIq,
            LocationResolutionSource.Geoapify]);
    }

    [Fact]
    public async Task FailOverOnceForTransientFailureButNotForEmptySuccess()
    {
        var geoapify = new FakeProvider("geoapify", LocationResolutionSource.Geoapify)
        {
            Responder = _ => GeocodingResult.Failed(GeocodingStatus.ProviderUnavailable)
        };
        var locationIq = new FakeProvider("locationiq", LocationResolutionSource.LocationIq);
        var selector = CreateSelector(geoapify, locationIq);

        var result = await selector.ForwardGeocodeAsync(Request(), CancellationToken.None);

        result.Provider.ShouldBe("locationiq");
        geoapify.CallCount.ShouldBe(1);
        locationIq.CallCount.ShouldBe(1);

        geoapify.Responder = _ => new GeocodingResult(GeocodingStatus.Success, []);
        await selector.ForwardGeocodeAsync(Request(), CancellationToken.None);
        await selector.ForwardGeocodeAsync(Request(), CancellationToken.None);
        geoapify.CallCount.ShouldBe(2);
        locationIq.CallCount.ShouldBe(2);
    }

    [Fact]
    public async Task SkipRateLimitedProviderDuringCooldown()
    {
        var geoapify = new FakeProvider("geoapify", LocationResolutionSource.Geoapify)
        {
            Responder = _ => GeocodingResult.Failed(GeocodingStatus.RateLimited, TimeSpan.FromMinutes(1))
        };
        var locationIq = new FakeProvider("locationiq", LocationResolutionSource.LocationIq);
        var selector = CreateSelector(geoapify, locationIq);

        await selector.ForwardGeocodeAsync(Request(), CancellationToken.None);
        await selector.ForwardGeocodeAsync(Request(), CancellationToken.None);
        await selector.ForwardGeocodeAsync(Request(), CancellationToken.None);

        geoapify.CallCount.ShouldBe(1);
        locationIq.CallCount.ShouldBe(3);
    }

    [Fact]
    public void RejectDuplicateAndUnknownConfiguration()
    {
        var known = new HashSet<string>(["geoapify", "locationiq"], StringComparer.Ordinal);
        var duplicates = Options(["geoapify", "Geoapify"]);
        var unknown = Options(["other"]);

        Should.Throw<InvalidOperationException>(() => duplicates.Validate(known));
        Should.Throw<InvalidOperationException>(() => unknown.Validate(known));
    }

    private static RoundRobinGeocodingProvider CreateSelector(params IGeocodingProviderClient[] providers) =>
        new(
            providers,
            Microsoft.Extensions.Options.Options.Create(Options(providers.Select(provider => provider.ProviderName).ToArray())),
            TimeProvider.System);

    private static GeocodingProviderSelectionOptions Options(IReadOnlyList<string> providers) => new()
    {
        Providers = providers,
        MaximumRateLimitCooldown = TimeSpan.FromMinutes(5)
    };

    private static GeocodingRequest Request() => new("Ogui Road", "Enugu", "Enugu", "NG", 5);

    private sealed class FakeProvider(string providerName, LocationResolutionSource resolutionSource) : IGeocodingProviderClient
    {
        public int CallCount { get; private set; }
        public string ProviderName => providerName;
        public LocationResolutionSource ResolutionSource => resolutionSource;
        public Func<GeocodingRequest, GeocodingResult> Responder { get; set; } = _ => new(GeocodingStatus.Success, []);

        public Task<GeocodingResult> ForwardGeocodeAsync(GeocodingRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(Responder(request));
        }
    }
}
