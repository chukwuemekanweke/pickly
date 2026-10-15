using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Application.Locations;

public sealed record NearbyLocationResponse(
    Guid Id,
    string DisplayAddress,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationType LocationType,
    decimal Latitude,
    decimal Longitude,
    int DistanceMeters)
{
    public static NearbyLocationResponse FromReadModel(NearbyLocationReadModel nearby) =>
        new(
            nearby.Location.Id,
            nearby.Location.DisplayAddress,
            nearby.Location.LocationType,
            nearby.Location.Latitude,
            nearby.Location.Longitude,
            (int)Math.Round(nearby.DistanceMeters, MidpointRounding.AwayFromZero));
}
