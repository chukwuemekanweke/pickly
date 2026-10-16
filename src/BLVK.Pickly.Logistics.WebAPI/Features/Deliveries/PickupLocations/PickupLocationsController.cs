using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.DeletePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.ListPickupLocations;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route(EndpointUrl.PickupLocations.Route)]
public sealed class PickupLocationsController(
    CreatePickupLocationHandler createHandler,
    ListPickupLocationsHandler listHandler,
    GetDefaultPickupLocationHandler getDefaultHandler,
    GetPickupLocationHandler getHandler,
    UpdatePickupLocationHandler updateHandler,
    SetDefaultPickupLocationHandler setDefaultHandler,
    DeletePickupLocationHandler deleteHandler,
    CreatePickupLocationRequestValidator createValidator,
    UpdatePickupLocationRequestValidator updateValidator,
    ListPickupLocationsRequestValidator listValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PickupLocationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PickupLocationResponse>> Create(
        [FromBody] CreatePickupLocationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await createHandler.HandleAsync(
            new CreatePickupLocationCommand(
                request.LocationId,
                request.Label,
                request.AdditionalDirections,
                request.IsDefault,
                ResolveActorContext()),
            cancellationToken);

        switch (result.Status)
        {
            case CreatePickupLocationStatus.NotAuthenticated:
                return Unauthorized();
            case CreatePickupLocationStatus.LocationNotFound:
                return LocationNotFound();
            case CreatePickupLocationStatus.LocationUnusable:
                return LocationUnusable();
        }

        var location = result.Location!;
        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", pickupLocationId = location.Id },
            location);
    }

    [HttpGet]
    [ProducesResponseType<ListPickupLocationsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ListPickupLocationsResponse>> List(
        [FromQuery] ListPickupLocationsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await listValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await listHandler.HandleAsync(
            new ListPickupLocationsQuery(request.Limit, request.Cursor, ResolveActorContext()),
            cancellationToken);

        return result.Status == ListPickupLocationsStatus.NotAuthenticated
            ? Unauthorized()
            : Ok(new ListPickupLocationsResponse(result.Items, result.NextCursor, result.HasMore));
    }

    [HttpGet("default")]
    [ProducesResponseType<PickupLocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PickupLocationResponse>> GetDefault(CancellationToken cancellationToken)
    {
        var result = await getDefaultHandler.HandleAsync(
            new GetDefaultPickupLocationQuery(ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            GetDefaultPickupLocationStatus.NotAuthenticated => Unauthorized(),
            GetDefaultPickupLocationStatus.NoDefault => NoContent(),
            _ => Ok(result.Location)
        };
    }

    [HttpGet("{pickupLocationId:guid}")]
    [ProducesResponseType<PickupLocationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PickupLocationResponse>> GetById(
        Guid pickupLocationId,
        CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(
            new GetPickupLocationQuery(pickupLocationId, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            GetPickupLocationStatus.NotAuthenticated => Unauthorized(),
            GetPickupLocationStatus.NotFound => NotFound(),
            _ => Ok(result.Location)
        };
    }

    [HttpPut("{pickupLocationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid pickupLocationId,
        [FromBody] UpdatePickupLocationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await updateHandler.HandleAsync(
            new UpdatePickupLocationCommand(
                pickupLocationId,
                request.LocationId,
                request.Label,
                request.AdditionalDirections,
                ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            UpdatePickupLocationStatus.NotAuthenticated => Unauthorized(),
            UpdatePickupLocationStatus.NotFound => NotFound(),
            UpdatePickupLocationStatus.LocationNotFound => LocationNotFound(),
            UpdatePickupLocationStatus.LocationUnusable => LocationUnusable(),
            _ => NoContent()
        };
    }

    [HttpPut("{pickupLocationId:guid}/default")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(Guid pickupLocationId, CancellationToken cancellationToken)
    {
        var result = await setDefaultHandler.HandleAsync(
            new SetDefaultPickupLocationCommand(pickupLocationId, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            SetDefaultPickupLocationStatus.NotAuthenticated => Unauthorized(),
            SetDefaultPickupLocationStatus.NotFound => NotFound(),
            _ => NoContent()
        };
    }

    [HttpDelete("{pickupLocationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid pickupLocationId, CancellationToken cancellationToken)
    {
        var result = await deleteHandler.HandleAsync(
            new DeletePickupLocationCommand(pickupLocationId, ResolveActorContext()),
            cancellationToken);

        return result.Status switch
        {
            DeletePickupLocationStatus.NotAuthenticated => Unauthorized(),
            DeletePickupLocationStatus.NotFound => NotFound(),
            _ => NoContent()
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

    private static ObjectResult LocationNotFound() =>
        AuthenticationProblemDetails.Create(
            StatusCodes.Status400BadRequest,
            PickupLocationErrorCodes.LocationNotFound,
            "Location not found",
            "The referenced location does not exist. Resolve or select a location before saving it.");

    private static ObjectResult LocationUnusable() =>
        AuthenticationProblemDetails.Create(
            StatusCodes.Status400BadRequest,
            PickupLocationErrorCodes.LocationUnusable,
            "Location unavailable",
            "The referenced location is no longer available for pickups. Choose another location.");
}
