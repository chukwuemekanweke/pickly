namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

public static class LocationErrorCodes
{
    public const string Unresolved = "location_unresolved";
    public const string UnsupportedRegion = "location_region_unsupported";
    public const string ResolutionInProgress = "location_resolution_in_progress";
    public const string SelectionNotFound = "location_selection_not_found";
    public const string LocationUnusable = "location_unusable";
    public const string GeocodingRateLimited = "geocoding_rate_limited";
    public const string GeocodingUnavailable = "geocoding_unavailable";
    public const string PermanentGeocodingUnavailable = "permanent_geocoding_unavailable";
    public const string InvalidProviderResponse = "geocoding_invalid_response";
}
