using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderOfferAcceptancePersistence
{
    Task<RiderOfferPreview?> FindPreviewAsync(Guid offerId, CancellationToken cancellationToken);

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

public sealed record RiderOfferPreview(Guid DeliveryId, Guid RiderProfileId);

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
