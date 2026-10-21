using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BLVK.Pickly.Logistics.Infrastructure.Locations;

internal sealed class RoundRobinGeocodingProvider : ILocationGeocodingProvider
{
    private readonly IReadOnlyList<IGeocodingProviderClient> _providers;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldowns = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _maximumCooldown;
    private int _counter = -1;

    public RoundRobinGeocodingProvider(
        IEnumerable<IGeocodingProviderClient> providers,
        IOptions<GeocodingProviderSelectionOptions> options,
        TimeProvider timeProvider)
    {
        var configured = options.Value.Providers.Select(GeocodingProviderSelectionOptions.Normalize).ToArray();
        var byName = providers.ToDictionary(provider => provider.ProviderName, StringComparer.OrdinalIgnoreCase);
        _providers = configured.Select(name => byName[name]).ToArray();
        _maximumCooldown = options.Value.MaximumRateLimitCooldown;
        _timeProvider = timeProvider;
    }

    public string ProviderName => "round-robin";
    public LocationResolutionSource ResolutionSource => default;

    public async Task<GeocodingResult> ForwardGeocodeAsync(
        GeocodingRequest request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var start = (int)(unchecked((uint)Interlocked.Increment(ref _counter)) % (uint)_providers.Count);
        var available = OrderedFrom(start)
            .Where(provider => !_cooldowns.TryGetValue(provider.ProviderName, out var until) || until <= now)
            .ToArray();

        if (available.Length == 0)
        {
            var next = _cooldowns
                .Where(pair => _providers.Any(provider => string.Equals(provider.ProviderName, pair.Key, StringComparison.Ordinal)))
                .MinBy(pair => pair.Value);
            return GeocodingResult.Failed(
                GeocodingStatus.RateLimited,
                next.Value - now,
                next.Key,
                _providers.First(provider => provider.ProviderName == next.Key).ResolutionSource);
        }

        GeocodingResult? lastFailure = null;
        GeocodingResult? rateLimitedFailure = null;
        foreach (var provider in available)
        {
            var result = await provider.ForwardGeocodeAsync(request, cancellationToken);
            result = result with { Provider = provider.ProviderName, ResolutionSource = provider.ResolutionSource };
            if (result.Status == GeocodingStatus.RateLimited)
            {
                var delay = result.RetryAfter.GetValueOrDefault(_maximumCooldown);
                if (delay <= TimeSpan.Zero || delay > _maximumCooldown)
                {
                    delay = _maximumCooldown;
                }

                _cooldowns[provider.ProviderName] = _timeProvider.GetUtcNow() + delay;
                rateLimitedFailure ??= result with { RetryAfter = delay };
            }

            if (!CanFailOver(result.Status))
            {
                return result;
            }

            lastFailure = result;
        }

        return rateLimitedFailure ?? lastFailure!;
    }

    private IEnumerable<IGeocodingProviderClient> OrderedFrom(int start)
    {
        for (var offset = 0; offset < _providers.Count; offset++)
        {
            yield return _providers[(start + offset) % _providers.Count];
        }
    }

    private static bool CanFailOver(GeocodingStatus status) => status is
        GeocodingStatus.RateLimited or
        GeocodingStatus.Timeout or
        GeocodingStatus.ProviderUnavailable;
}
