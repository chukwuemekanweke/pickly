using BLVK.Pickly.Logistics.Domain.Locations;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using Microsoft.Extensions.Options;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

/// <summary>
/// Searches only Pickly's registry. It never calls the geocoding provider, and only publicly discoverable locations
/// are returned, so private premise addresses and sender metadata cannot leak through global search.
/// </summary>
public sealed class SearchLocationsHandler(
    ILocationReadModelRepository repository,
    IOptions<LocationResolutionOptions> options)
{
    public async Task<SearchLocationsResult> HandleAsync(
        SearchLocationsQuery query,
        CancellationToken cancellationToken)
    {
        var stakeholderId = query.ActorContext.StakeholderId;
        if (!stakeholderId.HasValue || stakeholderId.Value == Guid.Empty)
        {
            return new SearchLocationsResult(SearchLocationsStatus.NotAuthenticated, []);
        }

        var terms = LocationSearchTerms.Extract(query.Query);
        if (terms.Count == 0)
        {
            return new SearchLocationsResult(SearchLocationsStatus.Success, []);
        }

        var limit = Math.Clamp(query.Limit, 1, options.Value.MaxResults);
        var countryCode = string.IsNullOrWhiteSpace(query.CountryCode)
            ? null
            : query.CountryCode.Trim().ToUpperInvariant();
        var locations = await repository.SearchAsync(
            new LocationSearchRequest(terms, countryCode, limit),
            cancellationToken);

        return new SearchLocationsResult(
            SearchLocationsStatus.Success,
            locations.Select(LocationSummaryResponse.FromReadModel).ToArray());
    }
}
