using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Common.Observability;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetVehicleOptions;

public sealed class GetVehicleOptionsHandler(DeliveryQuotePreparationService preparationService, ICustomTelemetryContext telemetry)
{
    public async Task<GetVehicleOptionsResult> HandleAsync(GetVehicleOptionsQuery query, CancellationToken cancellationToken)
    {
        var prepared = await preparationService.PrepareAsync(query.ActorContext.StakeholderId, query.PickupLocationId, query.DropOffLocationId, query.DeliveryType, query.OrderType, query.ScheduledPickupAtUtc, query.Items, null, cancellationToken);
        if (prepared.Status != DeliveryQuotePreparationStatus.Success)
        {
            telemetry.AddCustomEvent(Observability.EventNames.Deliveries.VehicleOptionsCalculated, new() { [Observability.PropertyNames.Deliveries.Outcome] = prepared.Status.ToString(), [Observability.PropertyNames.Deliveries.VehicleOptionCount] = "0" });
            return new(prepared.Status);
        }
        var ranked = prepared.Options!.OrderBy(x => x.Pricing.TotalAmount).ThenBy(x => x.Vehicle.MaximumWeightKg).ThenBy(x => x.Vehicle.Code, StringComparer.Ordinal).ThenBy(x => x.Vehicle.Id).ToArray();
        var recommended = ranked[0].Vehicle.Id;
        telemetry.AddCustomEvent(Observability.EventNames.Deliveries.VehicleOptionsCalculated, new() { [Observability.PropertyNames.Deliveries.Outcome] = "Success", [Observability.PropertyNames.Deliveries.VehicleOptionCount] = ranked.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        return new(DeliveryQuotePreparationStatus.Success, recommended, ranked.Select(x => new VehicleOptionResponse(x.Vehicle.Id, x.Vehicle.Code, x.Vehicle.Name, x.Vehicle.Id == recommended, new VehiclePriceEstimateResponse(x.Pricing.TotalAmount, x.CurrencyCode))).ToArray());
    }
}
