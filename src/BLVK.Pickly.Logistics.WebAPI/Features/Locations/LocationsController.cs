using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;
using BLVK.Pickly.Logistics.Application.Locations.Features.SearchLocations;
using BLVK.Pickly.Logistics.Application.Locations.Features.SelectLocationCandidate;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Globalization;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Locations;

/// <summary>
/// Shared location registry endpoints. Search uses the registry first and falls back to Mapbox; registration only
/// accepts an opaque candidate token issued by search. No endpoint accepts client-supplied coordinates.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Locations.Route)]
public sealed class LocationsController(
    SearchLocationsHandler searchHandler,
    SelectLocationCandidateHandler selectHandler,
    GetLocationHandler getHandler,
    GetNearbyLocationsHandler nearbyHandler,
    SearchLocationsRequestValidator searchValidator,
    RegisterLocationRequestValidator registerValidator,
    GetNearbyLocationsRequestValidator nearbyValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("search")]
    [EnableRateLimiting(RateLimitingPolicyNames.LocationResolutionPolicy)]
    [ProducesResponseType<LocationListResponse<LocationSearchItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<LocationListResponse<LocationSearchItemResponse>>> Search(
        [FromQuery] SearchLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await searchValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await searchHandler.HandleAsync(
            new SearchLocationsQuery(request.Q!, request.CountryCode, request.Limit, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            SearchLocationsStatus.Success => Ok(new LocationListResponse<LocationSearchItemResponse>(result.Items)),
            SearchLocationsStatus.NotAuthenticated => Unauthorized(),
            SearchLocationsStatus.UnsupportedRegion => Problem(
                StatusCodes.Status422UnprocessableEntity,
                LocationErrorCodes.UnsupportedRegion,
                "Region not supported",
                "Pickly does not serve this country yet."),
            SearchLocationsStatus.ResolutionInProgress => WithRetryAfter(
                result.RetryAfter,
                Problem(
                    StatusCodes.Status409Conflict,
                    LocationErrorCodes.ResolutionInProgress,
                    "Search in progress",
                    "The same location is being searched by another request. Retry shortly.")),
            SearchLocationsStatus.ProviderRateLimited => WithRetryAfter(
                result.RetryAfter,
                Problem(
                    StatusCodes.Status503ServiceUnavailable,
                    LocationErrorCodes.GeocodingRateLimited,
                    "Geocoding temporarily limited",
                    "Location search is temporarily rate limited. Retry later.")),
            SearchLocationsStatus.InvalidProviderResponse => Problem(
                StatusCodes.Status502BadGateway,
                LocationErrorCodes.InvalidProviderResponse,
                "Geocoding provider error",
                "The geocoding provider returned an unusable response."),
            _ => WithRetryAfter(
                result.RetryAfter,
                Problem(
                    StatusCodes.Status503ServiceUnavailable,
                    LocationErrorCodes.GeocodingUnavailable,
                    "Geocoding unavailable",
                    "The geocoding provider is temporarily unavailable. Retry later."))
        };
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicyNames.LocationResolutionPolicy)]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<LocationResponse>> Register(
        [FromBody] RegisterLocationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await selectHandler.HandleAsync(
            new SelectLocationCandidateCommand(request.SelectionToken, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            SelectLocationCandidateStatus.Resolved when result.Created => CreatedAtAction(
                nameof(GetById),
                new { locationId = result.Location!.LocationId },
                result.Location),
            SelectLocationCandidateStatus.Resolved => Ok(result.Location),
            SelectLocationCandidateStatus.NotAuthenticated => Unauthorized(),
            SelectLocationCandidateStatus.LocationUnusable => Problem(
                StatusCodes.Status422UnprocessableEntity,
                LocationErrorCodes.LocationUnusable,
                "Location unavailable",
                "The selected location is no longer available. Choose another location."),
            _ => Problem(
                StatusCodes.Status404NotFound,
                LocationErrorCodes.SelectionNotFound,
                "Selection not found",
                "The selection has expired or is not valid. Search again.")
        };
    }

    [HttpGet("nearby")]
    [ProducesResponseType<LocationListResponse<NearbyLocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationListResponse<NearbyLocationResponse>>> Nearby(
        [FromQuery] GetNearbyLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await nearbyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await nearbyHandler.HandleAsync(
            new GetNearbyLocationsQuery(
                request.LocationId,
                request.RadiusMeters,
                request.Limit,
                request.LocationType,
                ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            GetNearbyLocationsStatus.NotAuthenticated => Unauthorized(),
            GetNearbyLocationsStatus.AnchorNotFound => NotFound(),
            _ => Ok(new LocationListResponse<NearbyLocationResponse>(result.Items))
        };
    }

    [HttpGet("{locationId:guid}")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> GetById(Guid locationId, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(
            new GetLocationQuery(locationId, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            GetLocationStatus.NotAuthenticated => Unauthorized(),
            GetLocationStatus.NotFound => NotFound(),
            _ => Ok(result.Location)
        };
    }

    private ActorContext ResolveActorContext() =>
        Guid.TryParse(currentActor.ActorId, out var stakeholderId)
            ? new ActorContext(
                stakeholderId,
                currentActor.TenantId,
                currentActor.CorrelationId,
                currentActor.FlowId)
            : ActorContext.FromAnonymousActor(currentActor);

    private static ObjectResult Problem(
        int statusCode,
        string code,
        string title,
        string detail,
        IReadOnlyDictionary<string, object?>? extensions = null) =>
        AuthenticationProblemDetails.Create(statusCode, code, title, detail, extensions);

    private ObjectResult WithRetryAfter(TimeSpan? retryAfter, ObjectResult result)
    {
        if (retryAfter is { } delay && delay > TimeSpan.Zero)
        {
            Response.Headers.RetryAfter = Math.Ceiling(delay.TotalSeconds).ToString("F0", CultureInfo.InvariantCulture);
        }

        return result;
    }
}
