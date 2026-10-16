using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Application.Locations;

public sealed record LocationResponse(
    Guid LocationId,
    string DisplayAddress,
    string AddressLine,
    string City,
    string State,
    string CountryCode,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationType LocationType,
    LocationCoordinatesResponse Coordinates,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationResolutionSource ResolutionSource)
{
    public static LocationResponse FromEntity(Location location) =>
        new(
            location.Id,
            location.Address.DisplayAddress,
            location.Address.AddressLine,
            location.Address.City,
            location.Address.State,
            location.Address.CountryCode,
            location.LocationType,
            new LocationCoordinatesResponse(location.Coordinates.Latitude, location.Coordinates.Longitude),
            location.ResolutionSource);

    public static LocationResponse FromReadModel(LocationReadModel location) =>
        new(
            location.Id,
            location.DisplayAddress,
            location.AddressLine,
            location.City,
            location.State,
            location.CountryCode,
            location.LocationType,
            new LocationCoordinatesResponse(location.Latitude, location.Longitude),
            location.ResolutionSource);
}
