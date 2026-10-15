namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public enum ResolveLocationStatus
{
    Resolved = 1,
    SelectionRequired = 2,
    Unresolved = 3,
    NotAuthenticated = 4,
    UnsupportedRegion = 5,
    ResolutionInProgress = 6,
    ProviderRateLimited = 7,
    ProviderUnavailable = 8,
    PermanentGeocodingUnavailable = 9,
    InvalidProviderResponse = 10
}
