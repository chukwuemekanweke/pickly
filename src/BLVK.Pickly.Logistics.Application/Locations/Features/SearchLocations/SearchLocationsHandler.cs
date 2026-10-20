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
/// Searches Pickly's public registry first and falls back to permanent provider geocoding only when the registry does
/// not contain a selectable match. Provider candidates remain server-held until explicitly registered.
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
        var selectableRegistryCount = registryItems.Count(item => item.IsSelectable);
        var registryProperties = SearchProperties(query, locations.Count, selectableRegistryCount);
        if (selectableRegistryCount > 0)
        {
            registryProperties[Observability.PropertyNames.Locations.RegistryOutcome] = "selectable_matches";
            telemetry.Record(
                query.ActorContext,
                countryCode,
                Observability.EventNames.Locations.RegistryHit,
                registryProperties);
            RecordSearchPerformed(query, countryCode, registryItems.Length, "registry", registryProperties);
            return new SearchLocationsResult(SearchLocationsStatus.Success, registryItems);
        }

        registryProperties[Observability.PropertyNames.Locations.RegistryOutcome] = locations.Count == 0
            ? "no_matches"
            : "no_selectable_matches";
        telemetry.Record(
            query.ActorContext,
            countryCode,
            locations.Count == 0
                ? Observability.EventNames.Locations.RegistryMiss
                : Observability.EventNames.Locations.RegistryNoSelectableMatches,
            registryProperties);
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
        var providerResponseFromCache = outcome is not null;
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
                IReadOnlyList<LocationSearchItemResponse> responseItems =
                    geocoding.Status is GeocodingStatus.Timeout or GeocodingStatus.ProviderUnavailable
                    ? []
                    : registryItems;
                RecordSearchPerformed(
                    query,
                    countryCode,
                    responseItems.Count,
                    responseItems.Count == 0 ? "none" : "registry",
                    ProviderProperties(query, registryItems, geocoding.Candidates.Count, false, geocoding.Status.ToString()));
                return MapProviderFailure(geocoding, responseItems);
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
            RecordSearchPerformed(
                query,
                countryCode,
                fallback.Length,
                area is null ? (fallback.Length == 0 ? "none" : "registry") : "provider",
                ProviderProperties(query, registryItems, 0, providerResponseFromCache, "success"));
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
            var ambiguousProperties = ProviderProperties(
                query,
                registryItems,
                candidates.Length,
                providerResponseFromCache,
                "selection_required");
            telemetry.Record(
                query.ActorContext,
                countryCode,
                Observability.EventNames.Locations.ResolutionAmbiguous,
                ambiguousProperties);
        }

        var providerItems = candidates
            .Select((candidate, index) => LocationSearchItemResponse.FromMapbox(
                candidate,
                LocationSelectionTokens.Create(selectionId, index)))
            .ToArray();
        var registryCapacity = Math.Max(0, limit - providerItems.Length);
        var items = effectiveRegistryItems
            .Take(registryCapacity)
            .Concat(providerItems)
            .Take(limit)
            .ToArray();
        RecordSearchPerformed(
            query,
            countryCode,
            items.Length,
            effectiveRegistryItems.Count == 0 ? "provider" : "mixed",
            ProviderProperties(query, registryItems, candidates.Length, providerResponseFromCache, "success"));
        return new SearchLocationsResult(
            SearchLocationsStatus.Success,
            items);
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
        foreach (var property in telemetry.CreateSearchProperties(query.Query, "search"))
        {
            properties[property.Key] = property.Value;
        }
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
        GeocodingStatus.Timeout or GeocodingStatus.ProviderUnavailable => new SearchLocationsResult(
            SearchLocationsStatus.Success,
            registryItems),
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

    private Dictionary<string, string> SearchProperties(
        SearchLocationsQuery query,
        int registryResultCount,
        int selectableRegistryResultCount)
    {
        var properties = telemetry.CreateSearchProperties(query.Query, "search");
        properties[Observability.PropertyNames.Locations.RegistryResultCount] =
            registryResultCount.ToString(CultureInfo.InvariantCulture);
        properties[Observability.PropertyNames.Locations.SelectableRegistryResultCount] =
            selectableRegistryResultCount.ToString(CultureInfo.InvariantCulture);
        return properties;
    }

    private Dictionary<string, string> ProviderProperties(
        SearchLocationsQuery query,
        IReadOnlyCollection<LocationSearchItemResponse> registryItems,
        int providerCandidateCount,
        bool fromCache,
        string outcome)
    {
        var properties = SearchProperties(query, registryItems.Count, registryItems.Count(item => item.IsSelectable));
        properties[Observability.PropertyNames.Locations.Provider] = geocodingProvider.ProviderName;
        properties[Observability.PropertyNames.Locations.ProviderCandidateCount] =
            providerCandidateCount.ToString(CultureInfo.InvariantCulture);
        properties[Observability.PropertyNames.Locations.CandidateCount] =
            providerCandidateCount.ToString(CultureInfo.InvariantCulture);
        properties[Observability.PropertyNames.Locations.ProviderResponseFromCache] =
            fromCache.ToString(CultureInfo.InvariantCulture);
        properties[Observability.PropertyNames.Locations.Outcome] = outcome;
        return properties;
    }

    private void RecordSearchPerformed(
        SearchLocationsQuery query,
        string countryCode,
        int resultCount,
        string resultSource,
        IReadOnlyDictionary<string, string> context)
    {
        var properties = new Dictionary<string, string>(context)
        {
            [Observability.PropertyNames.Locations.ResultCount] = resultCount.ToString(CultureInfo.InvariantCulture),
            [Observability.PropertyNames.Locations.ResultSource] = resultSource
        };
        telemetry.Record(
            query.ActorContext,
            countryCode,
            Observability.EventNames.Locations.SearchPerformed,
            properties);
    }
}
