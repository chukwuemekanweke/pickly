using BLVK.Pickly.Logistics.Domain.Common.Observability;
using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.Persistence;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

/// <summary>Coordinates registry lookup and cross-instance lease ownership before provider resolution.</summary>
public sealed class ResolveLocationHandler(
    LocationRegistryMatcher registryMatcher,
    ILocationResolutionLease resolutionLease,
    ProviderLocationResolver providerResolver,
    ResolveLocationTelemetry telemetry,
    IOptions<LocationResolutionOptions> options,
    TimeProvider timeProvider)
{
    private static readonly TimeSpan InProgressRetryAfter = TimeSpan.FromSeconds(2);

    public async Task<ResolveLocationResult> HandleAsync(
        ResolveLocationCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
        {
            return new ResolveLocationResult(ResolveLocationStatus.NotAuthenticated);
        }

        var settings = options.Value;
        if (!settings.IsSupportedCountry(command.CountryCode))
        {
            return new ResolveLocationResult(ResolveLocationStatus.UnsupportedRegion);
        }

        var countryCode = command.CountryCode.Trim().ToUpperInvariant();
        var request = command with { CountryCode = countryCode };
        var normalizedAddress = LocationAddress.NormalizeForIdentity(
            request.AddressLine, request.City, request.State, countryCode);
        var resolutionKey = LocationAddress.ComputeResolutionKey(
            request.AddressLine, request.City, request.State, countryCode);

        var registryMatch = await registryMatcher.FindAsync(resolutionKey, normalizedAddress, cancellationToken);
        if (registryMatch is not null)
        {
            return registryMatcher.ToResult(request, registryMatch);
        }

        telemetry.Record(request, Observability.EventNames.Locations.RegistryMiss);

        var waitDeadline = timeProvider.GetUtcNow() + settings.ResolutionLeaseWaitTimeout;
        var waitedForConcurrentResolution = false;
        while (true)
        {
            var leaseId = await resolutionLease.TryAcquireAsync(
                resolutionKey, settings.ResolutionLeaseDuration, cancellationToken);
            if (leaseId.HasValue)
            {
                try
                {
                    var leasedMatch = await registryMatcher.FindAsync(
                        resolutionKey, normalizedAddress, cancellationToken);
                    return leasedMatch is not null
                        ? registryMatcher.ToResult(request, leasedMatch)
                        : await providerResolver.ResolveAsync(request, resolutionKey, settings, cancellationToken);
                }
                finally
                {
                    await resolutionLease.ReleaseAsync(resolutionKey, leaseId.Value, CancellationToken.None);
                }
            }

            if (!waitedForConcurrentResolution)
            {
                waitedForConcurrentResolution = true;
                telemetry.Record(request, Observability.EventNames.Locations.DuplicateResolutionPrevented);
            }

            if (timeProvider.GetUtcNow() >= waitDeadline)
            {
                return new ResolveLocationResult(
                    ResolveLocationStatus.ResolutionInProgress,
                    RetryAfter: InProgressRetryAfter);
            }

            await Task.Delay(settings.ResolutionLeasePollInterval, timeProvider, cancellationToken);
            var concurrentMatch = await registryMatcher.FindAsync(
                resolutionKey, normalizedAddress, cancellationToken);
            if (concurrentMatch is not null)
            {
                return registryMatcher.ToResult(
                    request,
                    concurrentMatch with { MatchKind = "concurrent_resolution" });
            }
        }
    }
}
