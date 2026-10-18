using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDelivery;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Bookings;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicyNames.RequireActiveSession)]
[Route("api/v{version:apiVersion}/deliveries")]
public sealed class DeliveryBookingsController(
    CreateDeliveryHandler createHandler,
    GetDeliveryHandler getHandler,
    CreateDeliveryRequestValidator validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        }

        var result = await createHandler.HandleAsync(
            new(
                request.QuoteId,
                request.Recipient.Name,
                request.Recipient.PhoneNumber,
                request.DropOffInstructions,
                Actor()),
            cancellationToken);

        if (result.Status == CreateDeliveryStatus.Success)
        {
            return result.IsReplay
                ? Ok(result.Delivery)
                : CreatedAtAction(
                    nameof(Get),
                    new { version = "1.0", deliveryId = result.Delivery!.DeliveryId },
                    result.Delivery);
        }

        return result.Status switch
        {
            CreateDeliveryStatus.NotAuthenticated => Unauthorized(),
            CreateDeliveryStatus.NotFound => NotFound(),
            CreateDeliveryStatus.InvalidRequest => Problem(400, "invalid_delivery_request", "Invalid delivery request", "Recipient or delivery instructions are invalid."),
            CreateDeliveryStatus.QuoteExpired => Problem(409, "quote_expired", "Quote expired", "The quote expired before it could be booked."),
            CreateDeliveryStatus.QuoteConsumed => Problem(409, "quote_consumed", "Quote already used", "The quote has already been used for another booking."),
            CreateDeliveryStatus.ScheduledPickupExpired => Problem(409, "scheduled_pickup_expired", "Scheduled pickup unavailable", "The scheduled pickup time is no longer in the future."),
            _ => Problem(409, "booking_conflict", "Booking conflict", "The quote was concurrently booked or its persisted state conflicts with this request.")
        };
    }

    [HttpGet("{deliveryId:guid}")]
    public async Task<IActionResult> Get(Guid deliveryId, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new(deliveryId, Actor()), cancellationToken);
        return result.Status switch
        {
            GetDeliveryStatus.NotAuthenticated => Unauthorized(),
            GetDeliveryStatus.NotFound => NotFound(),
            _ => Ok(result.Delivery)
        };
    }

    private ActorContext Actor() => Guid.TryParse(currentActor.ActorId, out var id)
        ? new(id, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId)
        : ActorContext.FromAnonymousActor(currentActor);

    private static ObjectResult Problem(int status, string code, string title, string detail) =>
        AuthenticationProblemDetails.Create(status, code, title, detail);
}
