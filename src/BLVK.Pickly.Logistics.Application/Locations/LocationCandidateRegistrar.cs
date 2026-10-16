using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;

namespace BLVK.Pickly.Logistics.Application.Locations;

/// <summary>
/// Turns a validated street or landmark provider candidate into a canonical shared location, reusing an existing location
/// when the provider reference is already registered.
/// </summary>
public sealed class LocationCandidateRegistrar(
    ILocationRegistrationStore registrationStore,
    ICustomTelemetryContext telemetry,
    TimeProvider timeProvider)
{
    public static bool IsRegistrable(GeocodingCandidate candidate, string requestedCountryCode) =>
        candidate.Precision is GeocodingPrecision.Street or GeocodingPrecision.Landmark &&
        string.Equals(candidate.CountryCode, requestedCountryCode, StringComparison.OrdinalIgnoreCase) &&
        TryBuildAddress(candidate, out _) &&
        GeographicCoordinates.IsValid(candidate.Latitude, candidate.Longitude);

    public async Task<Location> RegisterAsync(
        GeocodingCandidate candidate,
        string provider,
        LocationResolutionSource resolutionSource,
        string? resolutionKey,
        ActorContext actorContext,
        CancellationToken cancellationToken) =>
        (await RegisterWithOutcomeAsync(
            candidate,
            provider,
            resolutionSource,
            resolutionKey,
            actorContext,
            cancellationToken)).Location;

    public async Task<LocationRegistration> RegisterWithOutcomeAsync(
        GeocodingCandidate candidate,
        string provider,
        LocationResolutionSource resolutionSource,
        string? resolutionKey,
        ActorContext actorContext,
        CancellationToken cancellationToken)
    {
        if (!TryBuildAddress(candidate, out var address))
        {
            throw new InvalidOperationException("Only validated provider candidates can be registered.");
        }

        var location = Location.RegisterProviderResolved(
            address,
            GeographicCoordinates.Create(candidate.Latitude, candidate.Longitude),
            LocationVisibility.Public,
            resolutionSource,
            provider,
            candidate.FeatureId,
            candidate.FeatureType,
            timeProvider.GetUtcNow());

        var registration = await registrationStore.RegisterAsync(
            location,
            provider,
            candidate.FeatureId,
            resolutionKey,
            cancellationToken);

        telemetry.AddCustomEvent(
            registration.Created
                ? Observability.EventNames.Locations.Registered
                : Observability.EventNames.Locations.Reused,
            ObservabilityEventProperties.Create(
                actorContext,
                actorContext.StakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Locations.LocationId] = registration.Location.Id.ToString(),
                    [Observability.PropertyNames.Locations.Provider] = provider,
                    [Observability.PropertyNames.Locations.MatchKind] = "provider_reference"
                }));

        return registration;
    }

    private static bool TryBuildAddress(GeocodingCandidate candidate, out LocationAddress address)
    {
        try
        {
            address = LocationAddress.Create(
                candidate.AddressLine,
                candidate.City,
                candidate.State,
                candidate.CountryCode,
                candidate.DisplayAddress);
            return !string.IsNullOrWhiteSpace(candidate.FeatureId);
        }
        catch (ArgumentException)
        {
            address = null!;
            return false;
        }
    }
}
