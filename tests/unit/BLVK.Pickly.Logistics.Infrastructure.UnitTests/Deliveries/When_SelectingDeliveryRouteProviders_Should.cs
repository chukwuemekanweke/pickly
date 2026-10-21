using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Infrastructure.Deliveries;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Deliveries;

public sealed class When_SelectingDeliveryRouteProviders_Should
{
    [Fact]
    public async Task MaintainAnIndependentConfiguredRoundRobinSequence()
    {
        var geoapify = new FakeProvider("geoapify", ["driving", "cycling"]);
        var locationIq = new FakeProvider("locationiq", ["driving"]);
        var selector = CreateSelector(geoapify, locationIq);

        var providers = new List<string>();
        for (var index = 0; index < 5; index++)
        {
            providers.Add((await selector.GetRouteAsync(Request(), CancellationToken.None)).Provider);
        }

        providers.ShouldBe(["geoapify", "locationiq", "geoapify", "locationiq", "geoapify"]);
    }

    [Fact]
    public async Task FailOverTransientlyAndExcludeUnsupportedProfiles()
    {
        var geoapify = new FakeProvider("geoapify", ["driving", "cycling"]);
        var locationIq = new FakeProvider("locationiq", ["driving"])
        {
            Responder = _ => new DeliveryRouteResult(DeliveryRouteStatus.ProviderUnavailable)
        };
        var selector = CreateSelector(geoapify, locationIq);

        await selector.GetRouteAsync(Request(), CancellationToken.None);
        var failover = await selector.GetRouteAsync(Request(), CancellationToken.None);
        var cycling = await selector.GetRouteAsync(Request("cycling"), CancellationToken.None);

        failover.Provider.ShouldBe("geoapify");
        cycling.Provider.ShouldBe("geoapify");
        locationIq.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReturnNoRouteWithoutTryingAnotherProvider()
    {
        var geoapify = new FakeProvider("geoapify", ["driving"])
        {
            Responder = _ => new DeliveryRouteResult(DeliveryRouteStatus.NoRoute)
        };
        var locationIq = new FakeProvider("locationiq", ["driving"]);
        var selector = CreateSelector(geoapify, locationIq);

        var result = await selector.GetRouteAsync(Request(), CancellationToken.None);

        result.Status.ShouldBe(DeliveryRouteStatus.NoRoute);
        geoapify.CallCount.ShouldBe(1);
        locationIq.CallCount.ShouldBe(0);
    }

    private static RoundRobinDeliveryRouteProvider CreateSelector(params IRoutingProviderClient[] providers) =>
        new(
            providers,
            new MemoryJsonCache(),
            Options.Create(new DeliveryRouteProviderSelectionOptions
            {
                Providers = providers.Select(provider => provider.ProviderName).ToArray(),
                MaximumRateLimitCooldown = TimeSpan.FromMinutes(5)
            }),
            Options.Create(RoutingOptions()),
            TimeProvider.System,
            NullLogger<RoundRobinDeliveryRouteProvider>.Instance);

    private static DeliveryRoutingOptions RoutingOptions() => new()
    {
        BaseUrl = "https://api.mapbox.test/",
        AccessToken = "test",
        Timeout = TimeSpan.FromSeconds(5),
        MaxRetries = 0,
        MaximumCacheAge = TimeSpan.FromHours(24),
        CoordinationLeaseDuration = TimeSpan.FromSeconds(15),
        CoordinationWaitTimeout = TimeSpan.FromSeconds(20),
        CoordinationPollInterval = TimeSpan.FromMilliseconds(100),
        DefaultRateLimitRetryAfter = TimeSpan.FromSeconds(30),
        ServiceAreaTimeZone = "Africa/Lagos",
        ScheduleVersion = "test",
        Windows =
        [
            new RoutingWindowOptions { Id = "day", Start = TimeSpan.Zero, End = TimeSpan.FromHours(12), ApplicableDays = Enum.GetValues<DayOfWeek>() },
            new RoutingWindowOptions { Id = "night", Start = TimeSpan.FromHours(12), End = TimeSpan.Zero, ApplicableDays = Enum.GetValues<DayOfWeek>() }
        ]
    };

    private static DeliveryRouteRequest Request(string profile = "driving") => new(
        Guid.CreateVersion7(),
        GeographicCoordinates.FromDegrees(6.45, 7.51),
        Guid.CreateVersion7(),
        GeographicCoordinates.FromDegrees(6.46, 7.52),
        profile,
        new DateTimeOffset(2026, 10, 21, 10, 0, 0, TimeSpan.Zero));

    private sealed class FakeProvider(string providerName, IReadOnlyCollection<string> profiles) : IRoutingProviderClient
    {
        public int CallCount { get; private set; }
        public string ProviderName => providerName;
        public Func<DeliveryRouteRequest, DeliveryRouteResult> Responder { get; set; } = _ =>
            new(DeliveryRouteStatus.Success, 1000, TimeSpan.FromMinutes(2));

        public bool SupportsProfile(string profile) => profiles.Contains(profile);

        public Task<DeliveryRouteResult> GetRouteAsync(DeliveryRouteRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return Task.FromResult(Responder(request));
        }
    }

    private sealed class MemoryJsonCache : IJsonCache
    {
        private readonly Dictionary<string, object> _values = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken) =>
            Task.FromResult(_values.TryGetValue(key, out var value) ? (T?)value : default);

        public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
        {
            _values[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public async Task<T?> GetAndRemoveAsync<T>(string key, CancellationToken cancellationToken)
        {
            var value = await GetAsync<T>(key, cancellationToken);
            _values.Remove(key);
            return value;
        }
    }
}
