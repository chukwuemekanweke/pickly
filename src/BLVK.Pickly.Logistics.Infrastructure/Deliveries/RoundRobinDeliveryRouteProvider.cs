using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BLVK.Pickly.Logistics.Infrastructure.Deliveries;

internal sealed class RoundRobinDeliveryRouteProvider : IDeliveryRouteProvider
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.Ordinal);
    private readonly IJsonCache _cache;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _cooldowns = new(StringComparer.Ordinal);
    private readonly IDistributedLease? _distributedLease;
    private readonly ILogger<RoundRobinDeliveryRouteProvider> _logger;
    private readonly TimeSpan _maximumCooldown;
    private readonly DeliveryRoutingOptions _options;
    private readonly IReadOnlyList<IRoutingProviderClient> _providers;
    private readonly TimeProvider _timeProvider;
    private int _counter = -1;

    public RoundRobinDeliveryRouteProvider(
        IEnumerable<IRoutingProviderClient> providers,
        IJsonCache cache,
        IOptions<DeliveryRouteProviderSelectionOptions> selectionOptions,
        IOptions<DeliveryRoutingOptions> options,
        TimeProvider timeProvider,
        ILogger<RoundRobinDeliveryRouteProvider> logger,
        IDistributedLease? distributedLease = null)
    {
        var configured = selectionOptions.Value.Providers.Select(DeliveryRouteProviderSelectionOptions.Normalize).ToArray();
        var byName = providers.ToDictionary(provider => provider.ProviderName, StringComparer.OrdinalIgnoreCase);
        _providers = configured.Select(name => byName[name]).ToArray();
        _cache = cache;
        _distributedLease = distributedLease;
        _logger = logger;
        _maximumCooldown = selectionOptions.Value.MaximumRateLimitCooldown;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<DeliveryRouteResult> GetRouteAsync(DeliveryRouteRequest request, CancellationToken cancellationToken)
    {
        var profile = request.RoutingProfile?.Trim().ToLowerInvariant() ?? string.Empty;
        var eligible = _providers.Where(provider => provider.SupportsProfile(profile)).ToArray();
        if (eligible.Length == 0)
        {
            return new DeliveryRouteResult(DeliveryRouteStatus.UnsupportedProfile, RoutingProfile: profile);
        }

        var window = RoutingWindowResolver.Resolve(request.DepartureAtUtc, _options);
        var cached = await TryGetAnyCachedAsync(eligible, request, profile, window, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var gateKey = BuildCoordinationKey(request, profile, window);
        var gate = Gates.GetOrAdd(gateKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            cached = await TryGetAnyCachedAsync(eligible, request, profile, window, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }

            string? owner = null;
            try
            {
                if (_distributedLease is not null)
                {
                    owner = await _distributedLease.TryAcquireAsync(
                        $"{gateKey}:lease",
                        _options.CoordinationLeaseDuration,
                        cancellationToken);
                    var waitUntil = _timeProvider.GetUtcNow() + _options.CoordinationWaitTimeout;
                    while (owner is null && _timeProvider.GetUtcNow() < waitUntil)
                    {
                        await Task.Delay(_options.CoordinationPollInterval, _timeProvider, cancellationToken);
                        cached = await TryGetAnyCachedAsync(eligible, request, profile, window, cancellationToken);
                        if (cached is not null)
                        {
                            return cached;
                        }

                        owner = await _distributedLease.TryAcquireAsync(
                            $"{gateKey}:lease",
                            _options.CoordinationLeaseDuration,
                            cancellationToken);
                    }
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning("Redis route coordination is unavailable; routing is degraded to per-instance coalescing");
                owner = null;
            }

            try
            {
                return await CallProvidersAsync(eligible, request, profile, window, cancellationToken);
            }
            finally
            {
                if (owner is not null && _distributedLease is not null)
                {
                    try
                    {
                        await _distributedLease.ReleaseAsync($"{gateKey}:lease", owner, CancellationToken.None);
                    }
                    catch (Exception)
                    {
                        _logger.LogWarning("Redis route lease release failed; the bounded lease will expire");
                    }
                }
            }
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1)
            {
                Gates.TryRemove(gateKey, out _);
            }
        }
    }

    private async Task<DeliveryRouteResult> CallProvidersAsync(
        IReadOnlyList<IRoutingProviderClient> eligible,
        DeliveryRouteRequest request,
        string profile,
        RoutingWindowContext window,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var globalStart = (int)(unchecked((uint)Interlocked.Increment(ref _counter)) % (uint)_providers.Count);
        var ordered = OrderedFrom(globalStart)
            .Where(provider => provider.SupportsProfile(profile))
            .Where(provider => !_cooldowns.TryGetValue(provider.ProviderName, out var until) || until <= now)
            .ToArray();
        if (ordered.Length == 0)
        {
            var next = _cooldowns
                .Where(pair => eligible.Any(provider => provider.ProviderName == pair.Key))
                .MinBy(pair => pair.Value);
            return new DeliveryRouteResult(
                DeliveryRouteStatus.RateLimited,
                Provider: next.Key,
                RoutingProfile: profile,
                RetryAfter: next.Value - now);
        }

        DeliveryRouteResult? lastFailure = null;
        DeliveryRouteResult? rateLimitedFailure = null;
        foreach (var provider in ordered)
        {
            var result = await provider.GetRouteAsync(request with { RoutingProfile = profile }, cancellationToken);
            result = result with { Provider = provider.ProviderName, RoutingProfile = profile };
            if (result.Status == DeliveryRouteStatus.RateLimited)
            {
                var delay = result.RetryAfter.GetValueOrDefault(_maximumCooldown);
                if (delay <= TimeSpan.Zero || delay > _maximumCooldown)
                {
                    delay = _maximumCooldown;
                }

                _cooldowns[provider.ProviderName] = _timeProvider.GetUtcNow() + delay;
                rateLimitedFailure ??= result with { RetryAfter = delay };
            }

            if (result.Status == DeliveryRouteStatus.Success)
            {
                await TryCacheAsync(result, request, profile, window, cancellationToken);
                return result;
            }

            if (!CanFailOver(result.Status))
            {
                return result;
            }

            lastFailure = result;
        }

        return rateLimitedFailure ?? lastFailure!;
    }

    private IEnumerable<IRoutingProviderClient> OrderedFrom(int start)
    {
        for (var offset = 0; offset < _providers.Count; offset++)
        {
            yield return _providers[(start + offset) % _providers.Count];
        }
    }

    private async Task<DeliveryRouteResult?> TryGetAnyCachedAsync(
        IEnumerable<IRoutingProviderClient> providers,
        DeliveryRouteRequest request,
        string profile,
        RoutingWindowContext window,
        CancellationToken cancellationToken)
    {
        foreach (var provider in providers)
        {
            try
            {
                var cached = await _cache.GetAsync<CachedRoute>(
                    BuildCacheKey(provider.ProviderName, request, profile, window),
                    cancellationToken);
                if (cached is not null &&
                    cached.CalculatedAtUtc + _options.MaximumCacheAge > _timeProvider.GetUtcNow() &&
                    cached.OriginLocationId == request.OriginLocationId &&
                    cached.DestinationLocationId == request.DestinationLocationId &&
                    cached.RoutingProfile == profile &&
                    cached.WindowId == window.Id &&
                    cached.ScheduleVersion == window.ScheduleVersion &&
                    cached.LocalDate == window.LocalDate)
                {
                    return cached.ToResult();
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning("Redis route cache read failed; routing will call a provider");
                return null;
            }
        }

        return null;
    }

    private async Task TryCacheAsync(
        DeliveryRouteResult result,
        DeliveryRouteRequest request,
        string profile,
        RoutingWindowContext window,
        CancellationToken cancellationToken)
    {
        var calculatedAt = _timeProvider.GetUtcNow();
        var retentionEnd = calculatedAt + _options.MaximumCacheAge;
        var ttl = (window.EndsAtUtc < retentionEnd ? window.EndsAtUtc : retentionEnd) - calculatedAt;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        try
        {
            await _cache.SetAsync(
                BuildCacheKey(result.Provider, request, profile, window),
                new CachedRoute(
                    result.DistanceMeters,
                    result.Duration?.TotalSeconds,
                    calculatedAt,
                    request.OriginLocationId,
                    request.DestinationLocationId,
                    result.Provider,
                    profile,
                    window.Id,
                    window.ScheduleVersion,
                    window.LocalDate),
                ttl,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning("Redis route cache write failed");
        }
    }

    internal static string BuildCacheKey(
        string provider,
        DeliveryRouteRequest request,
        string profile,
        RoutingWindowContext window) =>
        $"delivery-route:v3:{provider}:{request.OriginLocationId:N}:{request.DestinationLocationId:N}:{profile}:{window.ScheduleVersion}:{window.Id}:{window.LocalDate:yyyyMMdd}";

    private static string BuildCoordinationKey(
        DeliveryRouteRequest request,
        string profile,
        RoutingWindowContext window) =>
        $"delivery-route:v3:coordination:{request.OriginLocationId:N}:{request.DestinationLocationId:N}:{profile}:{window.ScheduleVersion}:{window.Id}:{window.LocalDate:yyyyMMdd}";

    private static bool CanFailOver(DeliveryRouteStatus status) => status is
        DeliveryRouteStatus.RateLimited or
        DeliveryRouteStatus.ProviderUnavailable or
        DeliveryRouteStatus.Timeout;

    private sealed record CachedRoute(
        long DistanceMeters,
        double? DurationSeconds,
        DateTimeOffset CalculatedAtUtc,
        Guid OriginLocationId,
        Guid DestinationLocationId,
        string Provider,
        string RoutingProfile,
        string WindowId,
        string ScheduleVersion,
        DateOnly LocalDate)
    {
        public DeliveryRouteResult ToResult() => new(
            DeliveryRouteStatus.Success,
            DistanceMeters,
            DurationSeconds.HasValue ? TimeSpan.FromSeconds(DurationSeconds.Value) : null,
            Provider,
            RoutingProfile);
    }
}
