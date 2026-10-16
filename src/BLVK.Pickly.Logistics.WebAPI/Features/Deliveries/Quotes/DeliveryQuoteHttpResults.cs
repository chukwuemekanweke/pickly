using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;

internal static class DeliveryQuoteHttpResults
{
    public static IActionResult From(ControllerBase controller, DeliveryQuotePreparationStatus status) => status switch
    {
        DeliveryQuotePreparationStatus.NotAuthenticated => controller.Unauthorized(),
        DeliveryQuotePreparationStatus.PickupNotFound or DeliveryQuotePreparationStatus.LocationNotFound => controller.NotFound(),
        DeliveryQuotePreparationStatus.InvalidItems or DeliveryQuotePreparationStatus.InvalidSchedule => Problem(400, "invalid_quote_request", "Invalid quote request", "The schedule or parcel details are invalid."),
        DeliveryQuotePreparationStatus.LocationUnusable => Problem(400, "location_unusable", "Location unavailable", "A referenced location is no longer usable."),
        DeliveryQuotePreparationStatus.UnsupportedServiceArea => Problem(422, "unsupported_service_area", "Unsupported service area", "Pickup or destination is outside all active delivery zones."),
        DeliveryQuotePreparationStatus.AmbiguousServiceArea => Problem(409, "ambiguous_service_area", "Ambiguous service area", "Active delivery zones overlap for a referenced location."),
        DeliveryQuotePreparationStatus.NoEligibleVehicle => Problem(422, "no_eligible_vehicle", "No eligible vehicle", "No active vehicle category can safely carry the shipment with available routing and pricing."),
        DeliveryQuotePreparationStatus.VehicleIneligible => Problem(422, "vehicle_ineligible", "Vehicle ineligible", "The selected vehicle category cannot carry this shipment or is inactive."),
        DeliveryQuotePreparationStatus.RouteUnavailable => Problem(422, "route_unavailable", "Route unavailable", "The routing provider could not find a road route."),
        DeliveryQuotePreparationStatus.RoutingDependencyUnavailable => Problem(503, "routing_dependency_unavailable", "Routing unavailable", "Trusted road routing is temporarily unavailable."),
        DeliveryQuotePreparationStatus.PricingUnavailable => Problem(422, "pricing_unavailable", "Pricing unavailable", "No complete pricing policy applies to this request."),
        DeliveryQuotePreparationStatus.PricingAmbiguous or DeliveryQuotePreparationStatus.PricingConfigurationError => Problem(500, "pricing_configuration_error", "Pricing configuration error", "Pricing configuration is ambiguous or incomplete."),
        _ => Problem(500, "quote_generation_failed", "Quote generation failed", "The quote could not be generated.")
    };

    private static ObjectResult Problem(int status, string code, string title, string detail) => AuthenticationProblemDetails.Create(status, code, title, detail);
}
