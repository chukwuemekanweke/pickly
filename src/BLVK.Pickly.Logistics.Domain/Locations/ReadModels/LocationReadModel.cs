using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Locations.ReadModels;

public sealed record LocationReadModel(
    Guid Id,
    string DisplayAddress,
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    decimal Latitude,
    decimal Longitude,
    LocationType LocationType,
    LocationResolutionSource ResolutionSource,
    LocationResolutionStatus ResolutionStatus,
    LocationVisibility Visibility);
