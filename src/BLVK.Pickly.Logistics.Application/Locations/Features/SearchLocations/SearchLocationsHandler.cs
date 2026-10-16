using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

/// <summary>
/// Searches Pickly's public registry first and falls back to permanent provider geocoding when the registry does not
/// contain a reliable full-address match. Provider candidates remain server-held until explicitly registered.
/// </summary>
public sealed class SearchLocationsHandler(
    ILocationReadModelRepository repository,
    ILocationGeocodingProvider geocodingProvider,
    ILocationResolutionLease resolutionLease,
    IJsonCache cache,
    ResolveLocationTelemetry telemetry,
    IOptions<LocationResolutionOptions> options,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan InProgressRetryAfter = TimeSpan.FromSeconds(2);

    public async Task<SearchLocationsResult> HandleAsync(
        SearchLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var stakeholderId = query.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
        {
            return new SearchLocationsResult(SearchLocationsStatus.NotAuthenticated, []);
        }

        var settings = options.Value;
        var countryCode = string.IsNullOrWhiteSpace(query.CountryCode)
            ? settings.SupportedCountryCodes[0].ToUpperInvariant()
            : query.CountryCode.Trim().ToUpperInvariant();
        if (!settings.IsSupportedCountry(countryCode))
        {
            return new SearchLocationsResult(SearchLocationsStatus.UnsupportedRegion, []);
        }

        var terms = LocationSearchTerms.Extract(query.Query);
        if (terms.Count == 0)
        {
            return new SearchLocationsResult(SearchLocationsStatus.Success, []);
        }

        var limit = Math.Clamp(query.Limit, 1, settings.MaxResults);
        var locations = await repository.SearchAsync(
            new LocationSearchRequest(terms, countryCode, limit),
            cancellationToken);
        var registryItems = locations.Select(LocationSearchItemResponse.FromRegistry).ToArray();
        if (locations.Any(location => IsReliableRegistryMatch(query.Query, location.DisplayAddress)))
        {
            telemetry.Record(query.ActorContext, countryCode, Observability.EventNames.Locations.RegistryHit);
            return new SearchLocationsResult(SearchLocationsStatus.Success, registryItems);
        }

        telemetry.Record(query.ActorContext, countryCode, Observability.EventNames.Locations.RegistryMiss);
        var resolutionKey = LocationAddress.ComputeResolutionKey(query.Query, string.Empty, string.Empty, countryCode);
        var deadline = timeProvider.GetUtcNow() + settings.ResolutionLeaseWaitTimeout;
        var recordedContention = false;

        while (true)
        {
            var leaseId = await resolutionLease.TryAcquireAsync(
                resolutionKey,
                settings.ResolutionLeaseDuration,
                cancellationToken);
            if (leaseId.HasValue)
            {
                try
                {
                    return await SearchProviderAsync(
                        query,
                        countryCode,
                        resolutionKey,
                        limit,
                        registryItems,
                        settings,
                        cancellationToken);
                }
                finally
                {
                    await resolutionLease.ReleaseAsync(resolutionKey, leaseId.Value, CancellationToken.None);
                }
            }

            if (!recordedContention)
            {
                recordedContention = true;
                telemetry.Record(
                    query.ActorContext,
                    countryCode,
                    Observability.EventNames.Locations.DuplicateResolutionPrevented);
            }

            if (timeProvider.GetUtcNow() >= deadline)
            {
                return new SearchLocationsResult(
                    SearchLocationsStatus.ResolutionInProgress,
                    registryItems,
                    InProgressRetryAfter);
            }

            await Task.Delay(settings.ResolutionLeasePollInterval, timeProvider, cancellationToken);
        }
    }

    private async Task<SearchLocationsResult> SearchProviderAsync(
        SearchLocationsQuery query,
        string countryCode,
        string resolutionKey,
        int limit,
        IReadOnlyList<LocationSearchItemResponse> registryItems,
        LocationResolutionOptions settings,
        CancellationToken cancellationToken)
    {
        var outcomeCacheKey = LocationSelectionTokens.OutcomeCacheKey(resolutionKey);
        var outcome = await cache.GetAsync<CachedGeocodingOutcome>(outcomeCacheKey, cancellationToken);
        if (outcome is null)
        {
            var startedAt = timeProvider.GetTimestamp();
            var geocoding = await geocodingProvider.ForwardGeocodeAsync(
                new GeocodingRequest(
                    string.Join(' ', LocationSearchTerms.Extract(query.Query)),
                    string.Empty,
                    string.Empty,
                    countryCode,
                    settings.MaxProviderCandidates),
                cancellationToken);
            var elapsed = timeProvider.GetElapsedTime(startedAt);
            RecordProviderOutcome(query, countryCode, geocoding, elapsed);
            if (geocoding.Status != GeocodingStatus.Success)
            {
                return MapProviderFailure(geocoding, registryItems);
            }

            outcome = new CachedGeocodingOutcome(
                geocodingProvider.ProviderName,
                geocodingProvider.ResolutionSource,
                geocoding.Candidates);
            await cache.SetAsync(outcomeCacheKey, outcome, settings.CandidateSelectionLifetime, cancellationToken);
        }

        var effectiveRegistryItems = registryItems.ToList();
        var registeredFeatureIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in outcome.Candidates
                     .Where(candidate => LocationCandidateRegistrar.IsRegistrable(candidate, countryCode))
                     .DistinctBy(candidate => candidate.FeatureId))
        {
            var existing = await repository.FindByProviderReferenceAsync(outcome.Provider, candidate.FeatureId, cancellationToken);
            if (existing is not null && effectiveRegistryItems.All(item => item.LocationId != existing.Id))
            {
                effectiveRegistryItems.Add(LocationSearchItemResponse.FromRegistry(existing));
            }
            if (existing is not null) registeredFeatureIds.Add(candidate.FeatureId);
        }

        var candidates = outcome.Candidates
            .Where(candidate => LocationCandidateRegistrar.IsRegistrable(candidate, countryCode))
            .DistinctBy(candidate => candidate.FeatureId)
            .Where(candidate => !registeredFeatureIds.Contains(candidate.FeatureId))
            .Where(candidate => effectiveRegistryItems.All(item => !string.Equals(
                item.DisplayAddress,
                candidate.DisplayAddress,
                StringComparison.OrdinalIgnoreCase)))
            .Take(Math.Min(limit, settings.MaxProviderCandidates))
            .ToArray();
        if (candidates.Length == 0)
        {
            var area = outcome.Candidates.FirstOrDefault(candidate =>
                candidate.Precision == GeocodingPrecision.Area &&
                string.Equals(candidate.CountryCode, countryCode, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(candidate.DisplayAddress));
            var fallback = area is null
                ? effectiveRegistryItems.ToArray()
                : effectiveRegistryItems.Concat([LocationSearchItemResponse.FromArea(area)]).Take(limit).ToArray();
            return new SearchLocationsResult(SearchLocationsStatus.Success, fallback);
        }

        var selectionId = LocationSelectionTokens.NewSelectionId();
        await cache.SetAsync(
            LocationSelectionTokens.SelectionCacheKey(selectionId),
            new PendingLocationSelection(
                query.ActorContext.StakeholderId!.Value,
                outcome.Provider,
                outcome.ResolutionSource,
                candidates),
            settings.CandidateSelectionLifetime,
            cancellationToken);

        if (candidates.Length > 1)
        {
            telemetry.Record(
                query.ActorContext,
                countryCode,
                Observability.EventNames.Locations.ResolutionAmbiguous,
                new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Locations.CandidateCount] =
                        candidates.Length.ToString(CultureInfo.InvariantCulture)
                });
        }

        var providerItems = candidates
            .Select((candidate, index) => LocationSearchItemResponse.FromMapbox(
                candidate,
                LocationSelectionTokens.Create(selectionId, index)))
            .ToArray();
        return new SearchLocationsResult(
            SearchLocationsStatus.Success,
            effectiveRegistryItems.Concat(providerItems).Take(limit).ToArray());
    }

    private void RecordProviderOutcome(
        SearchLocationsQuery query,
        string countryCode,
        GeocodingResult geocoding,
        TimeSpan elapsed)
    {
        var properties = new Dictionary<string, string>
        {
            [Observability.PropertyNames.Locations.Provider] = geocodingProvider.ProviderName,
            [Observability.PropertyNames.Locations.Outcome] = geocoding.Status.ToString(),
            [Observability.PropertyNames.Locations.CandidateCount] =
                geocoding.Candidates.Count.ToString(CultureInfo.InvariantCulture),
            [Observability.PropertyNames.Locations.DurationMilliseconds] =
                elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)
        };
        telemetry.Record(query.ActorContext, countryCode, Observability.EventNames.Locations.GeocodingRequested, properties);
        if (geocoding.Status != GeocodingStatus.Success)
        {
            telemetry.Record(query.ActorContext, countryCode, Observability.EventNames.Locations.GeocodingFailed, properties);
        }
    }

    private static SearchLocationsResult MapProviderFailure(
        GeocodingResult geocoding,
        IReadOnlyList<LocationSearchItemResponse> registryItems) => geocoding.Status switch
    {
        GeocodingStatus.RateLimited => new SearchLocationsResult(
            SearchLocationsStatus.ProviderRateLimited,
            registryItems,
            geocoding.RetryAfter),
        GeocodingStatus.InvalidResponse => new SearchLocationsResult(
            SearchLocationsStatus.InvalidProviderResponse,
            registryItems),
        _ => new SearchLocationsResult(
            SearchLocationsStatus.ProviderUnavailable,
            registryItems,
            geocoding.RetryAfter)
    };

    private static bool IsReliableRegistryMatch(string query, string displayAddress)
        => string.Equals(
            LocationSearchTerms.NormalizeForComparison(query),
            LocationSearchTerms.NormalizeForComparison(displayAddress),
            StringComparison.Ordinal);
}
