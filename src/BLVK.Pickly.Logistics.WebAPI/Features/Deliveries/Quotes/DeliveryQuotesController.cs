using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDeliveryQuote;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryQuote;
using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

[ApiController, ApiVersion("1.0"), Authorize, Route("api/v{version:apiVersion}/delivery-quotes")]
public sealed class DeliveryQuotesController(CreateDeliveryQuoteHandler createHandler, GetDeliveryQuoteHandler getHandler, CreateDeliveryQuoteRequestValidator validator, ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryQuoteRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        _ = Enum.TryParse<DeliveryType>(request.DeliveryType, true, out var deliveryType);
        _ = Enum.TryParse<DeliveryOrderType>(request.OrderType, true, out var orderType);
        var result = await createHandler.HandleAsync(new(request.PickupLocationId, request.DropOffLocationId, deliveryType, orderType, request.ScheduledPickupAtUtc, request.VehicleCategoryId, request.Items.Select(x => new DeliveryQuoteItemInput(x.Description, x.Quantity, x.WeightKg)).ToArray(), Actor()), cancellationToken);
        return result.Status == DeliveryQuotePreparationStatus.Success ? CreatedAtAction(nameof(Get), new { version = "1.0", quoteId = result.Quote!.QuoteId }, result.Quote) : DeliveryQuoteHttpResults.From(this, result.Status);
    }

    [HttpGet("{quoteId:guid}")]
    public async Task<IActionResult> Get(Guid quoteId, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new(quoteId, Actor()), cancellationToken);
        return result.Status switch { GetDeliveryQuoteStatus.NotAuthenticated => Unauthorized(), GetDeliveryQuoteStatus.NotFound => NotFound(), _ => Ok(result.Quote) };
    }
    private ActorContext Actor() => Guid.TryParse(currentActor.ActorId, out var id) ? new(id, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId) : ActorContext.FromAnonymousActor(currentActor);
}
