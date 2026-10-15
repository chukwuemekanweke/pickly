using BLVK.Pickly.Logistics.Domain.Locations.Services;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;

public sealed record LocationCandidateResponse(
    string SelectionToken,
    string DisplayAddress,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] GeocodingPrecision Precision,
    LocationCoordinatesResponse Coordinates);
