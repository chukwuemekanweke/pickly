using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using System.Globalization;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

/// <summary>Owns the provider, candidate-selection, registration, and landmark-fallback workflow.</summary>
public sealed class ProviderLocationResolver(
    ILocationGeocodingProvider geocodingProvider,
    LocationCandidateRegistrar registrar,
    ILocationReadModelRepository locationReadModels,
    IJsonCache cache,
    ResolveLocationTelemetry telemetry,
    TimeProvider timeProvider)
{
    public async Task<ResolveLocationResult> ResolveAsync(
        ResolveLocationCommand request,
        string resolutionKey,
        LocationResolutionOptions settings,
        CancellationToken cancellationToken)
    {
        var outcomeCacheKey = LocationSelectionTokens.OutcomeCacheKey(resolutionKey);
        var cachedOutcome = await cache.GetAsync<CachedGeocodingOutcome>(outcomeCacheKey, cancellationToken);

        string provider;
        LocationResolutionSource resolutionSource;
        IReadOnlyList<GeocodingCandidate> candidates;
        if (cachedOutcome is not null)
        {
            provider = cachedOutcome.Provider;
            resolutionSource = cachedOutcome.ResolutionSource;
            candidates = cachedOutcome.Candidates;
        }
        else
        {
            var startedAt = timeProvider.GetTimestamp();
            var geocoding = await geocodingProvider.ForwardGeocodeAsync(
                new GeocodingRequest(
                    request.AddressLine,
                    request.City,
                    request.State,
                    request.CountryCode,
                    settings.MaxProviderCandidates),
                cancellationToken);
            geocoding = EnsureProviderIdentity(geocoding);
            var elapsed = timeProvider.GetElapsedTime(startedAt);

            RecordProviderOutcome(request, geocoding, elapsed);
            if (geocoding.Status != GeocodingStatus.Success)
            {
                return MapProviderFailure(geocoding);
            }

            provider = geocoding.Provider;
            resolutionSource = geocoding.ResolutionSource;
            candidates = geocoding.Candidates;
        }

        var evaluation = GeocodingCandidateEvaluation.Evaluate(candidates, request.CountryCode);
        if (evaluation.AcceptedCandidate is not null)
        {
            var location = await registrar.RegisterAsync(
                evaluation.AcceptedCandidate,
                provider,
                resolutionSource,
                resolutionKey,
                request.ActorContext,
                cancellationToken);
            return location.IsUsable
                ? new ResolveLocationResult(ResolveLocationStatus.Resolved, LocationResponse.FromEntity(location))
                : await UnresolvedAsync(request, null, settings, cancellationToken);
        }

        if (cachedOutcome is null)
        {
            await cache.SetAsync(
                outcomeCacheKey,
                new CachedGeocodingOutcome(provider, resolutionSource, candidates),
                settings.CandidateSelectionLifetime,
                cancellationToken);
        }

        if (evaluation.SelectableCandidates.Count > 0)
        {
            return await RequireSelectionAsync(
                request,
                provider,
                resolutionSource,
                evaluation.SelectableCandidates,
                settings,
                cancellationToken);
        }

        return await UnresolvedAsync(request, evaluation.ApproximateAnchor, settings, cancellationToken);
    }

    private GeocodingResult EnsureProviderIdentity(GeocodingResult result) => result with
    {
        Provider = string.IsNullOrWhiteSpace(result.Provider) ? geocodingProvider.ProviderName : result.Provider,
        ResolutionSource = result.ResolutionSource == default
            ? geocodingProvider.ResolutionSource
            : result.ResolutionSource
    };

    private void RecordProviderOutcome(
        ResolveLocationCommand request,
        GeocodingResult geocoding,
        TimeSpan elapsed)
    {
        telemetry.Record(
            request,
            Observability.EventNames.Locations.GeocodingRequested,
            new Dictionary<string, string>
            {
                [Observability.PropertyNames.Locations.Provider] = geocoding.Provider,
                [Observability.PropertyNames.Locations.Outcome] = geocoding.Status.ToString(),
                [Observability.PropertyNames.Locations.CandidateCount] =
                    geocoding.Candidates.Count.ToString(CultureInfo.InvariantCulture),
                [Observability.PropertyNames.Locations.DurationMilliseconds] =
                    elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)
            });

        if (geocoding.Status != GeocodingStatus.Success)
        {
            telemetry.Record(
                request,
                Observability.EventNames.Locations.GeocodingFailed,
                new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Locations.Provider] = geocoding.Provider,
                    [Observability.PropertyNames.Locations.Outcome] = geocoding.Status.ToString()
                });
        }
    }

    private async Task<ResolveLocationResult> RequireSelectionAsync(
        ResolveLocationCommand request,
        string provider,
        LocationResolutionSource resolutionSource,
        IReadOnlyList<GeocodingCandidate> candidates,
        LocationResolutionOptions settings,
        CancellationToken cancellationToken)
    {
        var selectionId = LocationSelectionTokens.NewSelectionId();
        await cache.SetAsync(
            LocationSelectionTokens.SelectionCacheKey(selectionId),
            new PendingLocationSelection(
                request.ActorContext.StakeholderId!.Value,
                provider,
                resolutionSource,
                candidates),
            settings.CandidateSelectionLifetime,
            cancellationToken);

        telemetry.Record(
            request,
            Observability.EventNames.Locations.ResolutionAmbiguous,
            new Dictionary<string, string>
            {
                [Observability.PropertyNames.Locations.Provider] = provider,
                [Observability.PropertyNames.Locations.CandidateCount] =
                    candidates.Count.ToString(CultureInfo.InvariantCulture)
            });

        var responses = candidates
            .Select((candidate, index) => new LocationCandidateResponse(
                LocationSelectionTokens.Create(selectionId, index),
                candidate.DisplayAddress,
                candidate.Precision,
                new LocationCoordinatesResponse(candidate.Latitude, candidate.Longitude)))
            .ToArray();
        return new ResolveLocationResult(ResolveLocationStatus.SelectionRequired, Candidates: responses);
    }

    private async Task<ResolveLocationResult> UnresolvedAsync(
        ResolveLocationCommand request,
        GeographicCoordinates? approximateAnchor,
        LocationResolutionOptions settings,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NearbyLocationResponse> landmarks = [];
        if (approximateAnchor is not null)
        {
            var nearby = await locationReadModels.FindNearbyAsync(
                new NearbyLocationsRequest(
                    approximateAnchor,
                    settings.LandmarkFallbackRadiusMeters,
                    settings.LandmarkFallbackLimit,
                    null,
                    LocationType.Landmark),
                cancellationToken);
            landmarks = nearby.Select(NearbyLocationResponse.FromReadModel).ToArray();
        }

        telemetry.Record(
            request,
            Observability.EventNames.Locations.ResolutionFailed,
            new Dictionary<string, string>
            {
                [Observability.PropertyNames.Locations.Outcome] = ResolveLocationStatus.Unresolved.ToString(),
                [Observability.PropertyNames.Locations.CandidateCount] =
                    landmarks.Count.ToString(CultureInfo.InvariantCulture)
            });

        return new ResolveLocationResult(ResolveLocationStatus.Unresolved, NearbyLandmarks: landmarks);
    }

    private static ResolveLocationResult MapProviderFailure(GeocodingResult geocoding) => geocoding.Status switch
    {
        GeocodingStatus.RateLimited => new ResolveLocationResult(
            ResolveLocationStatus.ProviderRateLimited,
            RetryAfter: geocoding.RetryAfter),
        GeocodingStatus.InvalidResponse => new ResolveLocationResult(ResolveLocationStatus.InvalidProviderResponse),
        _ => new ResolveLocationResult(ResolveLocationStatus.ProviderUnavailable, RetryAfter: geocoding.RetryAfter)
    };
}
