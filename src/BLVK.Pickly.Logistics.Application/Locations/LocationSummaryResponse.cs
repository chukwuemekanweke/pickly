using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Application.Locations;

public sealed record LocationSummaryResponse(
    Guid Id,
    string DisplayAddress,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationType LocationType,
    decimal Latitude,
    decimal Longitude)
{
    public static LocationSummaryResponse FromReadModel(LocationReadModel location) =>
        new(location.Id, location.DisplayAddress, location.LocationType, location.Latitude, location.Longitude);
}
