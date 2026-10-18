using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderAvailabilityPersistence
{
    Task<RiderAvailabilityState> LoadAsync(
        RiderProfile profile,
        CancellationToken cancellationToken);
}

public sealed record RiderAvailabilityState(
    RiderCurrentLocation? Location,
    bool HasActiveDelivery,
    bool IsVehicleCategoryActive,
    IReadOnlyList<RiderDeliveryOffer> PendingOffers,
    IReadOnlyDictionary<Guid, DeliveryMatchingSession> MatchingSessions);
