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
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GeocodingPrecision? Precision,
    bool IsSelectable)
{
    public static LocationSearchItemResponse FromRegistry(LocationReadModel location) =>
        new(
            location.Id,
            null,
            location.DisplayAddress,
            LocationSearchSource.Registry,
            location.LocationType,
            null,
            location.LocationType is LocationType.Street or LocationType.Landmark);

    public static LocationSearchItemResponse FromProvider(GeocodingCandidate candidate, string selectionToken) =>
        new(null, selectionToken, candidate.DisplayAddress, LocationSearchSource.Provider,
            candidate.Precision == GeocodingPrecision.Landmark ? LocationType.Landmark : LocationType.Street,
            candidate.Precision, true);

    public static LocationSearchItemResponse FromArea(GeocodingCandidate candidate) =>
        new(null, null, candidate.DisplayAddress, LocationSearchSource.Provider, LocationType.Area,
            candidate.Precision, false);
}
