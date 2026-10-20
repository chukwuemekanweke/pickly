using BLVK.Pickly.Logistics.Domain.Common.Caching;
using BLVK.Pickly.Logistics.Domain.Common.Observability;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;

/// <summary>
/// Registers the candidate a sender explicitly chose from a server-held ambiguous resolution. The token only indexes
/// candidates the server stored for that sender, so clients cannot submit their own provider data or coordinates.
/// </summary>
public sealed class SelectLocationCandidateHandler(
    IJsonCache cache,
    LocationCandidateRegistrar registrar,
    ICustomTelemetryContext telemetry)
{
    public async Task<SelectLocationCandidateResult> HandleAsync(
        SelectLocationCandidateCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
        {
            return new SelectLocationCandidateResult(SelectLocationCandidateStatus.NotAuthenticated);
        }

        if (!LocationSelectionTokens.TryParse(command.SelectionToken, out var selectionId, out var candidateIndex))
        {
            return new SelectLocationCandidateResult(SelectLocationCandidateStatus.SelectionNotFound);
        }

        var selection = await cache.GetAsync<PendingLocationSelection>(
            LocationSelectionTokens.SelectionCacheKey(selectionId),
            cancellationToken);
        if (selection is null ||
            selection.StakeholderId != stakeholderId.Value ||
            candidateIndex < 0 ||
            candidateIndex >= selection.Candidates.Count)
        {
            return new SelectLocationCandidateResult(SelectLocationCandidateStatus.SelectionNotFound);
        }

        var candidate = selection.Candidates[candidateIndex];
        if (!LocationCandidateRegistrar.IsRegistrable(candidate, candidate.CountryCode))
        {
            return new SelectLocationCandidateResult(SelectLocationCandidateStatus.LocationUnusable);
        }

        // An explicitly chosen candidate is not recorded as the identity of the original request text: another
        // sender typing the same text may have meant a different candidate.
        var registration = await registrar.RegisterWithOutcomeAsync(
            candidate,
            selection.Provider,
            selection.ResolutionSource,
            resolutionKey: null,
            command.ActorContext,
            cancellationToken);
        if (!registration.Location.IsUsable)
        {
            return new SelectLocationCandidateResult(SelectLocationCandidateStatus.LocationUnusable);
        }

        telemetry.AddCustomEvent(
            Observability.EventNames.Locations.CandidateSelected,
            ObservabilityEventProperties.Create(
                command.ActorContext,
                stakeholderId,
                additionalProperties: new Dictionary<string, string>
                {
                    [Observability.PropertyNames.Locations.LocationId] = registration.Location.Id.ToString()
                }));

        return new SelectLocationCandidateResult(
            SelectLocationCandidateStatus.Resolved,
            LocationResponse.FromEntity(registration.Location),
            registration.Created);
    }
}
