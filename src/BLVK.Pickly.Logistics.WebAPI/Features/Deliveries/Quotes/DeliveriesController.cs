using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetVehicleOptions;
using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

[ApiController, ApiVersion("1.0"), Authorize, Route("api/v{version:apiVersion}/deliveries")]
public sealed class DeliveriesController(GetVehicleOptionsHandler handler, GetVehicleOptionsRequestValidator validator, ICurrentActor currentActor) : ControllerBase
{
    [HttpPost("vehicle-options")]
    public async Task<IActionResult> GetVehicleOptions([FromBody] GetVehicleOptionsRequest request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(new ValidationProblemDetails(validation.ToValidationDictionary()));
        _ = Enum.TryParse<DeliveryType>(request.DeliveryType, true, out var deliveryType);
        _ = Enum.TryParse<DeliveryOrderType>(request.OrderType, true, out var orderType);
        var result = await handler.HandleAsync(new(request.PickupLocationId, request.DropOffLocationId, deliveryType, orderType, request.ScheduledPickupAtUtc, request.Items.Select(x => new DeliveryQuoteItemInput(x.Description, x.Quantity, x.WeightKg)).ToArray(), Actor()), cancellationToken);
        return result.Status == DeliveryQuotePreparationStatus.Success ? Ok(new { recommendedVehicleCategoryId = result.RecommendedVehicleCategoryId, eligibleCategories = result.Options }) : DeliveryQuoteHttpResults.From(this, result.Status);
    }
    private ActorContext Actor() => Guid.TryParse(currentActor.ActorId, out var id) ? new(id, currentActor.TenantId, currentActor.CorrelationId, currentActor.FlowId) : ActorContext.FromAnonymousActor(currentActor);
}
