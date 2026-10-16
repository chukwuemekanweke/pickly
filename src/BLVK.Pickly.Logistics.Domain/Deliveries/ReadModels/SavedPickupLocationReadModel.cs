using BLVK.Pickly.Logistics.Domain.Locations.Enums;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public sealed record SavedPickupLocationReadModel(
    Guid Id,
    Guid LocationId,
    string Label,
    string? AdditionalDirections,
    bool IsDefault,
    DateTimeOffset CreatedAtUtc,
    string DisplayAddress,
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    LocationType LocationType,
    decimal Latitude,
    decimal Longitude);
