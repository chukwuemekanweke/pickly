using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
using BLVK.Pickly.Logistics.Domain.Locations.Enums;
using System.Text.Json.Serialization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

/// <summary>
/// Outcome of a resolution request. <c>Resolved</c> carries the Pickly location to use for pickup creation;
/// <c>SelectionRequired</c> carries candidates whose opaque tokens must be submitted to the selection endpoint.
/// </summary>
public sealed record ResolveLocationResponse(
    string Status,
    Guid? LocationId,
    string? DisplayAddress,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationType? LocationType,
    LocationCoordinatesResponse? Coordinates,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] LocationResolutionSource? ResolutionSource,
    IReadOnlyList<LocationCandidateResponse> Candidates)
{
    public const string ResolvedStatus = "Resolved";
    public const string SelectionRequiredStatus = "SelectionRequired";

    public static ResolveLocationResponse Resolved(LocationResponse location) =>
        new(
            ResolvedStatus,
            location.LocationId,
            location.DisplayAddress,
            location.LocationType,
            location.Coordinates,
            location.ResolutionSource,
            []);

    public static ResolveLocationResponse SelectionRequired(IReadOnlyList<LocationCandidateResponse> candidates) =>
        new(SelectionRequiredStatus, null, null, null, null, null, candidates);
}
