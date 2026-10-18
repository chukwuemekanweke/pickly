using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderActiveDelivery;
using BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderStatus;
using BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;
using BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;
using BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
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
    UpdateRiderLocationHandler updateLocationHandler,
    SetRiderAvailabilityHandler setAvailabilityHandler,
    GetRiderStatusHandler getStatusHandler,
    ListRiderOffersHandler listOffersHandler,
    AcceptRiderOfferHandler acceptOfferHandler,
    RejectRiderOfferHandler rejectOfferHandler,
    GetRiderActiveDeliveryHandler getActiveDeliveryHandler,
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

        var result = await updateLocationHandler.HandleAsync(
            new(request.Latitude, request.Longitude, request.CapturedAtUtc, Actor()),
            cancellationToken);
        return result.Status switch
        {
            UpdateRiderLocationStatus.Success => NoContent(),
            UpdateRiderLocationStatus.NotAuthenticated => Unauthorized(),
            UpdateRiderLocationStatus.NotRider => Forbid(),
            UpdateRiderLocationStatus.ProfileInactive => Problem(409, "rider_inactive", "Rider inactive", "The rider profile is not active and approved."),
            UpdateRiderLocationStatus.StaleLocation => Problem(409, "stale_rider_location", "Stale location", "A newer rider location has already been accepted."),
            UpdateRiderLocationStatus.TemporarilyUnavailable => TemporarilyUnavailable(),
            _ => Problem(400, "invalid_rider_location", "Invalid rider location", "The coordinates or capture time are invalid.")
        };
    }

    [HttpPut("availability")]
    public async Task<IActionResult> SetAvailability(
        [FromBody] RiderAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await setAvailabilityHandler.HandleAsync(new(request.IsAvailable, Actor()), cancellationToken);
        return result.Status switch
        {
            SetRiderAvailabilityStatus.Success => Ok(result.RiderStatus),
            SetRiderAvailabilityStatus.NotAuthenticated => Unauthorized(),
            SetRiderAvailabilityStatus.NotRider => Forbid(),
            SetRiderAvailabilityStatus.ProfileInactive => Problem(409, "rider_inactive", "Rider inactive", "The rider profile is not active and approved."),
            SetRiderAvailabilityStatus.StaleLocation => Problem(409, "stale_rider_location", "Fresh location required", "A fresh rider location is required before becoming available."),
            SetRiderAvailabilityStatus.TemporarilyUnavailable => TemporarilyUnavailable(),
            _ => Problem(409, "rider_status_conflict", "Rider status conflict", "The requested availability conflicts with an active assignment.")
        };
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var result = await getStatusHandler.HandleAsync(new(Actor()), cancellationToken);
        return result.Status switch
        {
            GetRiderStatusStatus.Success => Ok(result.RiderStatus),
            GetRiderStatusStatus.NotAuthenticated => Unauthorized(),
            GetRiderStatusStatus.NotRider => Forbid(),
            _ => Problem(409, "rider_status_conflict", "Rider status conflict", "The rider status could not be retrieved.")
        };
    }

    [HttpGet("delivery-offers")]
    public async Task<IActionResult> ListOffers(CancellationToken cancellationToken)
    {
        var result = await listOffersHandler.HandleAsync(new(Actor()), cancellationToken);
        return result.Status switch
        {
            ListRiderOffersStatus.Success => Ok(result.Offers),
            ListRiderOffersStatus.NotAuthenticated => Unauthorized(),
            ListRiderOffersStatus.NotRider => Forbid(),
            ListRiderOffersStatus.ProfileInactive => Problem(409, "rider_inactive", "Rider inactive", "The rider profile is not active and approved."),
            _ => Conflict()
        };
    }

    [HttpPost("delivery-offers/{offerId:guid}/accept")]
    public async Task<IActionResult> AcceptOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await acceptOfferHandler.HandleAsync(new(offerId, Actor()), cancellationToken);
        return result.Status switch
        {
            AcceptRiderOfferStatus.Success => Ok(result.Delivery),
            AcceptRiderOfferStatus.NotAuthenticated => Unauthorized(),
            AcceptRiderOfferStatus.NotRider or AcceptRiderOfferStatus.Forbidden => Forbid(),
            AcceptRiderOfferStatus.NotFound => NotFound(),
            AcceptRiderOfferStatus.Expired => Problem(409, "offer_expired", "Offer expired", "The delivery offer has expired."),
            AcceptRiderOfferStatus.TemporarilyUnavailable => TemporarilyUnavailable(),
            _ => Problem(409, "assignment_conflict", "Assignment conflict", "The offer, delivery, or rider is no longer eligible for assignment.")
        };
    }

    [HttpPost("delivery-offers/{offerId:guid}/reject")]
    public async Task<IActionResult> RejectOffer(Guid offerId, CancellationToken cancellationToken)
    {
        var result = await rejectOfferHandler.HandleAsync(new(offerId, Actor()), cancellationToken);
        return result.Status switch
        {
            RejectRiderOfferStatus.Success => NoContent(),
            RejectRiderOfferStatus.NotAuthenticated => Unauthorized(),
            RejectRiderOfferStatus.NotRider or RejectRiderOfferStatus.Forbidden => Forbid(),
            RejectRiderOfferStatus.NotFound => NotFound(),
            RejectRiderOfferStatus.Expired => Problem(409, "offer_expired", "Offer expired", "The delivery offer has expired."),
            RejectRiderOfferStatus.TemporarilyUnavailable => TemporarilyUnavailable(),
            _ => Conflict()
        };
    }

    [HttpGet("active-delivery")]
    public async Task<IActionResult> GetActiveDelivery(CancellationToken cancellationToken)
    {
        var result = await getActiveDeliveryHandler.HandleAsync(new(Actor()), cancellationToken);
        return result.Status switch
        {
            GetRiderActiveDeliveryStatus.Success => Ok(result.Delivery),
            GetRiderActiveDeliveryStatus.NotAuthenticated => Unauthorized(),
            GetRiderActiveDeliveryStatus.NotRider => Forbid(),
            GetRiderActiveDeliveryStatus.NotFound => NoContent(),
            _ => Conflict()
        };
    }

    private ActorContext Actor() => Guid.TryParse(currentActor.ActorId, out var id)
        ? new(id, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId)
        : ActorContext.FromAnonymousActor(currentActor);

    private static ObjectResult Problem(int status, string code, string title, string detail) =>
        AuthenticationProblemDetails.Create(status, code, title, detail);

    private static ObjectResult TemporarilyUnavailable() =>
        Problem(
            StatusCodes.Status503ServiceUnavailable,
            "rider_operation_temporarily_unavailable",
            "Rider operation temporarily unavailable",
            "The operation could not complete because of concurrent database activity. Please retry.");
}
