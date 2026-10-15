using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries;

public sealed record PickupLocationResponse(
    Guid Id,
    Guid LocationId,
    string Label,
    string? AdditionalDirections,
    bool IsDefault,
    PickupLocationAddress Address)
{
    public static PickupLocationResponse FromEntities(SavedPickupLocation savedLocation, Location location) =>
        new(
            savedLocation.Id,
            location.Id,
            savedLocation.Label,
            savedLocation.AdditionalDirections,
            savedLocation.IsDefault,
            new PickupLocationAddress(
                location.Address.DisplayAddress,
                location.Address.AddressLine,
                location.Address.City,
                location.Address.State,
                location.Address.CountryCode,
                location.LocationType,
                location.Coordinates.Latitude,
                location.Coordinates.Longitude));

    public static PickupLocationResponse FromReadModel(SavedPickupLocationReadModel savedLocation) =>
        new(
            savedLocation.Id,
            savedLocation.LocationId,
            savedLocation.Label,
            savedLocation.AdditionalDirections,
            savedLocation.IsDefault,
            new PickupLocationAddress(
                savedLocation.DisplayAddress,
                savedLocation.AddressLine,
                savedLocation.City,
                savedLocation.State,
                savedLocation.CountryCode,
                savedLocation.LocationType,
                savedLocation.Latitude,
                savedLocation.Longitude));
}
