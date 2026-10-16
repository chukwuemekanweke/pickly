using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Quotes;

public sealed class DeliveryQuotePreparationService(IRepository<SavedPickupLocation> pickupLocations, IRepository<Location> locations, IRepository<VehicleCategory> vehicles, IRepository<DeliveryPricingPolicy> policies, IRepository<Currency> currencies, IDeliveryZoneReadModelRepository zones, IDeliveryRouteProvider routeProvider, TimeProvider timeProvider)
{
    public async Task<DeliveryQuotePreparation> PrepareAsync(Guid? senderId, Guid pickupLocationId, Guid dropOffLocationId, DeliveryType deliveryType, DeliveryOrderType orderType, DateTimeOffset? scheduledPickupAtUtc, IReadOnlyCollection<DeliveryQuoteItemInput> items, Guid? selectedVehicleCategoryId, CancellationToken cancellationToken)
    {
        if (!senderId.HasValue || senderId == Guid.Empty) return new(DeliveryQuotePreparationStatus.NotAuthenticated);
        var now = timeProvider.GetUtcNow();
        if (!Enum.IsDefined(deliveryType) || !Enum.IsDefined(orderType) || (deliveryType == DeliveryType.Scheduled ? scheduledPickupAtUtc is null || scheduledPickupAtUtc <= now : scheduledPickupAtUtc is not null)) return new(DeliveryQuotePreparationStatus.InvalidSchedule);
        decimal totalWeight;
        try { totalWeight = DeliveryPricingCalculator.CalculateTotalWeight(items.Select(x => (x.Quantity, x.WeightKg))); }
        catch (Exception exception) when (exception is ArgumentException or OverflowException) { return new(DeliveryQuotePreparationStatus.InvalidItems); }
        var pickup = await pickupLocations.FirstOrDefaultAsync(new SavedPickupLocationByOwnerAndIdSpecification(senderId.Value, pickupLocationId), cancellationToken);
        if (pickup is null) return new(DeliveryQuotePreparationStatus.PickupNotFound);
        var origin = await locations.GetByIdAsync(pickup.LocationId, cancellationToken);
        var destination = dropOffLocationId == Guid.Empty ? null : await locations.GetByIdAsync(dropOffLocationId, cancellationToken);
        if (destination is null) return new(DeliveryQuotePreparationStatus.LocationNotFound);
        if (origin is null || !origin.IsUsable || !destination.IsUsable) return new(DeliveryQuotePreparationStatus.LocationUnusable);
        var originZones = await zones.FindCoveringAsync(origin.Coordinates, now, cancellationToken);
        var destinationZones = await zones.FindCoveringAsync(destination.Coordinates, now, cancellationToken);
        if (originZones.Count == 0 || destinationZones.Count == 0) return new(DeliveryQuotePreparationStatus.UnsupportedServiceArea);
        if (HasAmbiguousTopPriority(originZones) || HasAmbiguousTopPriority(destinationZones)) return new(DeliveryQuotePreparationStatus.AmbiguousServiceArea);
        var zonePair = new DeliveryZoneReadModels(originZones[0].Id, originZones[0].Code, destinationZones[0].Id, destinationZones[0].Code);
        var eligible = await vehicles.ListAsync(new EligibleVehicleCategoriesSpecification(totalWeight), cancellationToken);
        if (eligible.Count == 0) return new(DeliveryQuotePreparationStatus.NoEligibleVehicle);
        if (selectedVehicleCategoryId.HasValue && eligible.All(x => x.Id != selectedVehicleCategoryId)) return new(DeliveryQuotePreparationStatus.VehicleIneligible);
        if (selectedVehicleCategoryId.HasValue) eligible = eligible.Where(x => x.Id == selectedVehicleCategoryId).ToArray();
        var applicable = await policies.ListAsync(new ApplicableDeliveryPricingPoliciesSpecification(now, deliveryType, orderType), cancellationToken);
        if (applicable.Count == 0) return new(DeliveryQuotePreparationStatus.PricingUnavailable);
        var routeByProfile = new Dictionary<string, DeliveryRouteResult>(StringComparer.Ordinal);
        var prepared = new List<PreparedVehicleOption>();
        var hasCompletePricing = false;
        var hasNoRoute = false;
        var hasRoutingFailure = false;
        foreach (var vehicle in eligible)
        {
            var matching = applicable.Where(policy => policy.VehicleRules.Any(x => x.VehicleCategoryId == vehicle.Id) && policy.ZoneRules.Any(x => x.OriginZoneId == zonePair.OriginId && x.DestinationZoneId == zonePair.DestinationId)).ToArray();
            if (matching.Length == 0) continue;
            if (matching.Length > 1) return new(DeliveryQuotePreparationStatus.PricingAmbiguous);
            hasCompletePricing = true;
            if (!routeByProfile.TryGetValue(vehicle.RoutingProfile, out var route))
            {
                route = await routeProvider.GetRouteAsync(new(origin.Id, origin.Coordinates, destination.Id, destination.Coordinates, vehicle.RoutingProfile, scheduledPickupAtUtc ?? now), cancellationToken);
                routeByProfile[vehicle.RoutingProfile] = route;
            }
            if (route.Status != DeliveryRouteStatus.Success)
            {
                if (selectedVehicleCategoryId.HasValue) return new(route.Status == DeliveryRouteStatus.NoRoute ? DeliveryQuotePreparationStatus.RouteUnavailable : DeliveryQuotePreparationStatus.RoutingDependencyUnavailable);
                hasNoRoute |= route.Status == DeliveryRouteStatus.NoRoute;
                hasRoutingFailure |= route.Status != DeliveryRouteStatus.NoRoute;
                continue;
            }
            DeliveryPricingBreakdown price;
            try { price = DeliveryPricingCalculator.Calculate(matching[0], vehicle.Id, zonePair.OriginId, zonePair.DestinationId, route.DistanceMeters, totalWeight); }
            catch (Exception exception) when (exception is InvalidOperationException or OverflowException) { return new(DeliveryQuotePreparationStatus.PricingConfigurationError); }
            var currency = await currencies.GetByIdAsync(matching[0].CurrencyId, cancellationToken);
            if (currency is null || !currency.IsActive) return new(DeliveryQuotePreparationStatus.PricingConfigurationError);
            prepared.Add(new(vehicle, matching[0], price, route.DistanceMeters, currency.CurrencyCode));
        }
        if (prepared.Count == 0)
        {
            if (!hasCompletePricing) return new(DeliveryQuotePreparationStatus.PricingUnavailable);
            if (hasRoutingFailure) return new(DeliveryQuotePreparationStatus.RoutingDependencyUnavailable);
            if (hasNoRoute) return new(DeliveryQuotePreparationStatus.RouteUnavailable);
            return new(DeliveryQuotePreparationStatus.NoEligibleVehicle);
        }
        var pickupAddress = DeliveryAddress.Create(origin.Address.AddressLine, pickup.AdditionalDirections, origin.Address.City, origin.Address.State, origin.Address.CountryCode, origin.Coordinates.Latitude, origin.Coordinates.Longitude);
        var dropOffAddress = DeliveryAddress.Create(destination.Address.AddressLine, null, destination.Address.City, destination.Address.State, destination.Address.CountryCode, destination.Coordinates.Latitude, destination.Coordinates.Longitude);
        return new(DeliveryQuotePreparationStatus.Success, origin.Id, destination.Id, pickupAddress, dropOffAddress, zonePair, totalWeight, prepared);
    }

    private static bool HasAmbiguousTopPriority(IReadOnlyList<DeliveryZoneReadModel> matchingZones) =>
        matchingZones.Count > 1 && matchingZones[0].Priority == matchingZones[1].Priority;
}
