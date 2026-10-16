namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

public enum SearchLocationsStatus
{
    Success = 1,
    NotAuthenticated = 2,
    UnsupportedRegion = 3,
    ResolutionInProgress = 4,
    ProviderRateLimited = 5,
    ProviderUnavailable = 6,
    InvalidProviderResponse = 7
}
