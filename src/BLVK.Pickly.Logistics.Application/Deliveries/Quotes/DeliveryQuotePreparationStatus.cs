namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public enum DeliveryQuotePreparationStatus
{
    Success = 1, NotAuthenticated, PickupNotFound, LocationNotFound, LocationUnusable, InvalidSchedule, InvalidItems,
    UnsupportedServiceArea, AmbiguousServiceArea, NoEligibleVehicle, VehicleIneligible, PricingUnavailable,
    PricingAmbiguous, PricingConfigurationError, RouteUnavailable, RoutingDependencyUnavailable
}
