using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Locations;
using BLVK.Pickly.Logistics.Application.Locations.Features.GetLocation;
using BLVK.Pickly.Logistics.Application.Locations.Features.GetNearbyLocations;
using BLVK.Pickly.Logistics.Application.Locations.Features.ResolveLocation;
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
/// Shared location registry endpoints. Search, lookup and proximity read only Pickly's registry; only the resolve
/// endpoints may reach the paid geocoding provider, and they are rate limited per user. There is no endpoint that
/// accepts client-supplied coordinates.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.Locations.Route)]
public sealed class LocationsController(
    SearchLocationsHandler searchHandler,
    ResolveLocationHandler resolveHandler,
    SelectLocationCandidateHandler selectHandler,
    GetLocationHandler getHandler,
    GetNearbyLocationsHandler nearbyHandler,
    SearchLocationsRequestValidator searchValidator,
    ResolveLocationRequestValidator resolveValidator,
    SelectLocationCandidateRequestValidator selectValidator,
    GetNearbyLocationsRequestValidator nearbyValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType<LocationListResponse<LocationSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LocationListResponse<LocationSummaryResponse>>> Search(
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

        return result.Status == SearchLocationsStatus.NotAuthenticated
            ? Unauthorized()
            : Ok(new LocationListResponse<LocationSummaryResponse>(result.Items));
    }

    [HttpPost("resolve")]
    [EnableRateLimiting(RateLimitingPolicyNames.LocationResolutionPolicy)]
    [ProducesResponseType<ResolveLocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ResolveLocationResponse>> Resolve(
        [FromBody] ResolveLocationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await resolveValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await resolveHandler.HandleAsync(
            new ResolveLocationCommand(
                request.AddressLine,
                request.City,
                request.State,
                request.CountryCode,
                ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            ResolveLocationStatus.Resolved => Ok(ResolveLocationResponse.Resolved(result.Location!)),
            ResolveLocationStatus.SelectionRequired => Ok(ResolveLocationResponse.SelectionRequired(result.Candidates!)),
            ResolveLocationStatus.NotAuthenticated => Unauthorized(),
            ResolveLocationStatus.Unresolved => Problem(
                StatusCodes.Status422UnprocessableEntity,
                LocationErrorCodes.Unresolved,
                "Location could not be resolved",
                "The address could not be resolved to a precise location. Choose a known landmark or another resolvable location.",
                new Dictionary<string, object?> { ["nearbyLandmarks"] = result.NearbyLandmarks ?? [] }),
            ResolveLocationStatus.UnsupportedRegion => Problem(
                StatusCodes.Status422UnprocessableEntity,
                LocationErrorCodes.UnsupportedRegion,
                "Region not supported",
                "Pickly does not serve this country yet."),
            ResolveLocationStatus.ResolutionInProgress => WithRetryAfter(
                result.RetryAfter,
                Problem(
                    StatusCodes.Status409Conflict,
                    LocationErrorCodes.ResolutionInProgress,
                    "Resolution in progress",
                    "The same address is being resolved by another request. Retry shortly.")),
            ResolveLocationStatus.ProviderRateLimited => WithRetryAfter(
                result.RetryAfter,
                Problem(
                    StatusCodes.Status503ServiceUnavailable,
                    LocationErrorCodes.GeocodingRateLimited,
                    "Geocoding temporarily limited",
                    "Location resolution is temporarily rate limited. Retry later.")),
            ResolveLocationStatus.InvalidProviderResponse => Problem(
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

    [HttpPost("resolve/selections")]
    [EnableRateLimiting(RateLimitingPolicyNames.LocationResolutionPolicy)]
    [ProducesResponseType<ResolveLocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResolveLocationResponse>> SelectCandidate(
        [FromBody] SelectLocationCandidateRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await selectValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await selectHandler.HandleAsync(
            new SelectLocationCandidateCommand(request.SelectionToken, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            SelectLocationCandidateStatus.Resolved => Ok(ResolveLocationResponse.Resolved(result.Location!)),
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
                "The selection has expired or is not valid. Resolve the address again.")
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
