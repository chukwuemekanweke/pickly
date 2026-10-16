using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

/// <summary>
/// Classifies provider candidates. A candidate is accepted automatically only when it is the single confident,
/// registrable match; anything less certain is offered for an explicit choice, and coarse results are only ever used
/// as an approximate anchor for suggesting nearby known landmarks.
/// </summary>
public sealed record GeocodingCandidateEvaluation(
    GeocodingCandidate? AcceptedCandidate,
    IReadOnlyList<GeocodingCandidate> SelectableCandidates,
    GeographicCoordinates? ApproximateAnchor)
{
    public static GeocodingCandidateEvaluation Evaluate(
        IReadOnlyList<GeocodingCandidate> candidates,
        string requestedCountryCode)
    {
        var registrable = candidates
            .Where(candidate => LocationCandidateRegistrar.IsRegistrable(candidate, requestedCountryCode))
            .DistinctBy(candidate => candidate.FeatureId)
            .ToArray();

        var confident = registrable
            .Where(candidate => candidate.Confidence is GeocodingMatchConfidence.Exact or GeocodingMatchConfidence.High)
            .ToArray();
        var competing = registrable.Count(candidate => candidate.Confidence != GeocodingMatchConfidence.Low);

        if (confident.Length == 1 && competing == 1)
        {
            return new GeocodingCandidateEvaluation(confident[0], [], null);
        }

        if (registrable.Length > 0)
        {
            return new GeocodingCandidateEvaluation(null, registrable, null);
        }

        var anchor = candidates
            .Where(candidate =>
                candidate.Precision == GeocodingPrecision.Area &&
                string.Equals(candidate.CountryCode, requestedCountryCode, StringComparison.OrdinalIgnoreCase) &&
                GeographicCoordinates.IsValid(candidate.Latitude, candidate.Longitude))
            .Select(candidate => GeographicCoordinates.Create(candidate.Latitude, candidate.Longitude))
            .FirstOrDefault();

        return new GeocodingCandidateEvaluation(null, [], anchor);
    }
}
