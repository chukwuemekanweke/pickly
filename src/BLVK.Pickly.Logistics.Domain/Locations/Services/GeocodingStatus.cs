namespace BLVK.Pickly.Logistics.Domain.Locations.Services;

public enum GeocodingStatus
{
    Success = 1,
    RateLimited = 2,
    Timeout = 3,
    ProviderUnavailable = 4,
    InvalidResponse = 5
}
