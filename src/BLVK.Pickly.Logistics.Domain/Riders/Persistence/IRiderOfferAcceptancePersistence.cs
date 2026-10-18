using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferAcceptancePersistence
{
    Task<Delivery?> FindAcceptedDeliveryAsync(
        Guid offerId,
        Guid riderProfileId,
        Guid riderStakeholderId,
        CancellationToken cancellationToken);

    /// <summary>Loads tracked acceptance state; the Delivery row stays locked until the transaction ends.</summary>
    Task<RiderOfferAcceptanceState> LoadAsync(
        Guid offerId,
        Guid riderProfileId,
        CancellationToken cancellationToken);

    Task<bool> IsVehicleCategoryActiveAsync(Guid vehicleCategoryId, CancellationToken cancellationToken);

    Task<RiderGeographicEligibility> GetGeographicEligibilityAsync(
        RiderProfile profile,
        Delivery delivery,
        RiderCurrentLocation? location,
        int searchRadiusMeters,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);

    Task<bool> HasActiveDeliveryAsync(Guid riderStakeholderId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RiderDeliveryOffer>> ListConflictingOffersAsync(
        Guid acceptedOfferId,
        Guid deliveryId,
        Guid riderProfileId,
        CancellationToken cancellationToken);

    void AddStatusHistory(DeliveryStatusHistory history);
}

public sealed record RiderOfferAcceptanceState(
    RiderDeliveryOffer? Offer,
    Delivery? Delivery,
    DeliveryMatchingSession? MatchingSession,
    RiderCurrentLocation? Location);

public sealed record RiderGeographicEligibility(
    bool PickupZoneIsSupported,
    bool RiderServesPickupZone,
    bool IsWithinSearchRadius)
{
    public bool IsEligible => PickupZoneIsSupported && RiderServesPickupZone && IsWithinSearchRadius;
}
