using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Riders;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Riders;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route("api/v{version:apiVersion}/riders/me")]
public sealed class RidersController(
    RiderOperationsService service,
    RiderLocationRequestValidator locationValidator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPut("location")]
    [EnableRateLimiting(RateLimitingPolicyNames.RiderLocationPolicy)]
    public async Task<IActionResult> UpdateLocation(
        [FromBody] RiderLocationRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await locationValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await service.UpdateLocationAsync(
            Actor(),
            request.Latitude,
            request.Longitude,
            request.CapturedAtUtc,
            cancellationToken);
        return result.Code switch
        {
            RiderOperationCode.Success => NoContent(),
            RiderOperationCode.NotAuthenticated => Unauthorized(),
            RiderOperationCode.NotRider => Forbid(),
            RiderOperationCode.ProfileInactive => Problem(409, "rider_inactive", "Rider inactive", "The rider profile is not active and approved."),
            RiderOperationCode.StaleLocation => Problem(409, "stale_rider_location", "Stale location", "A newer rider location has already been accepted."),
            _ => Problem(400, "invalid_rider_location", "Invalid rider location", "The coordinates or capture time are invalid.")
        };
    }

    [HttpPut("availability")]
    public async Task<IActionResult> SetAvailability(
        [FromBody] RiderAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SetAvailabilityAsync(Actor(), request.IsAvailable, cancellationToken);
        return StatusResult(result);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken) =>
        StatusResult(await service.GetStatusAsync(Actor(), cancellationToken));

    [HttpGet("delivery-offers")]
    public async Task<IActionResult> ListOffers(CancellationToken cancellationToken)
    {
        var result = await service.ListOffersAsync(Actor(), cancellationToken);
        return result.Code switch
        {
            RiderOperationCode.Success => Ok(result.Offers),
            RiderOperationCode.NotAuthenticated => Unauthorized(),
            RiderOperationCode.NotRider => Forbid(),
            RiderOperationCode.ProfileInactive => Problem(409, "rider_inactive", "Rider inactive", "The rider profile is not active and approved."),
            _ => Conflict()
        };
    }

    [HttpPost("delivery-offers/{offerId:guid}/accept")]
    public async Task<IActionResult> AcceptOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await service.AcceptOfferAsync(Actor(), offerId, cancellationToken);
        return result.Code switch
        {
            RiderOperationCode.Success => Ok(result.Delivery),
            RiderOperationCode.NotAuthenticated => Unauthorized(),
            RiderOperationCode.NotRider or RiderOperationCode.Forbidden => Forbid(),
            RiderOperationCode.NotFound => NotFound(),
            RiderOperationCode.Expired => Problem(409, "offer_expired", "Offer expired", "The delivery offer has expired."),
            _ => Problem(409, "assignment_conflict", "Assignment conflict", "The offer, delivery, or rider is no longer eligible for assignment.")
        };
    }

    [HttpPost("delivery-offers/{offerId:guid}/reject")]
    public async Task<IActionResult> RejectOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await service.RejectOfferAsync(Actor(), offerId, cancellationToken);
        return result.Code switch
        {
            RiderOperationCode.Success => NoContent(),
            RiderOperationCode.NotAuthenticated => Unauthorized(),
            RiderOperationCode.NotRider or RiderOperationCode.Forbidden => Forbid(),
            RiderOperationCode.NotFound => NotFound(),
            RiderOperationCode.Expired => Problem(409, "offer_expired", "Offer expired", "The delivery offer has expired."),
            _ => Conflict()
        };
    }

    [HttpGet("active-delivery")]
    public async Task<IActionResult> GetActiveDelivery(CancellationToken cancellationToken)
    {
        var result = await service.GetActiveDeliveryAsync(Actor(), cancellationToken);
        return result.Code switch
        {
            RiderOperationCode.Success => Ok(result.Delivery),
            RiderOperationCode.NotAuthenticated => Unauthorized(),
            RiderOperationCode.NotRider => Forbid(),
            RiderOperationCode.NotFound => NoContent(),
            _ => Conflict()
        };
    }

    private IActionResult StatusResult(RiderStatusResult result) => result.Code switch
    {
        RiderOperationCode.Success => Ok(result.Status),
        RiderOperationCode.NotAuthenticated => Unauthorized(),
        RiderOperationCode.NotRider => Forbid(),
        RiderOperationCode.ProfileInactive => Problem(409, "rider_inactive", "Rider inactive", "The rider profile is not active and approved."),
        RiderOperationCode.StaleLocation => Problem(409, "stale_rider_location", "Fresh location required", "A fresh rider location is required before becoming available."),
        _ => Problem(409, "rider_status_conflict", "Rider status conflict", "The requested availability conflicts with an active assignment.")
    };

    private ActorContext Actor() => Guid.TryParse(currentActor.ActorId, out var id)
        ? new(id, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId)
        : ActorContext.FromAnonymousActor(currentActor);

    private static ObjectResult Problem(int status, string code, string title, string detail) =>
        AuthenticationProblemDetails.Create(status, code, title, detail);
}
