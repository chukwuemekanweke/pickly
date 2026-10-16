using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using BLVK.Pickly.Logistics.Domain.Locations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;

public sealed record LocationSearchItemResponse(
    Guid? LocationId,
    string? SelectionToken,
    string DisplayAddress,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationSearchSource Source,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationType LocationType,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GeocodingPrecision? Precision)
{
    public static LocationSearchItemResponse FromRegistry(LocationReadModel location) =>
        new(location.Id, null, location.DisplayAddress, LocationSearchSource.Registry, location.LocationType, null);

    public static LocationSearchItemResponse FromMapbox(GeocodingCandidate candidate, string selectionToken) =>
        new(null, selectionToken, candidate.DisplayAddress, LocationSearchSource.Mapbox, LocationType.Address, candidate.Precision);
}
