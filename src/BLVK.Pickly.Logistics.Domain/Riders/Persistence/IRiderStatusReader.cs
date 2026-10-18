using BLVK.Pickly.Logistics.Domain.Riders.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Persistence;

public interface IRiderStatusReader
{
    Task<RiderStatusFacts> GetFactsAsync(
        RiderProfile profile,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken);
}

public sealed record RiderStatusFacts(
    RiderCurrentLocation? Location,
    bool HasActiveDelivery,
    int PendingOfferCount);
