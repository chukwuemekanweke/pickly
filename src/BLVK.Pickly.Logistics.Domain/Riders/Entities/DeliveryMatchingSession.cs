using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.Riders.Entities;

public sealed class DeliveryMatchingSession : Entity, IAggregateRoot
{
    private DeliveryMatchingSession()
    {
    }

    private DeliveryMatchingSession(Guid deliveryId, int searchRadiusMeters, DateTimeOffset createdAtUtc)
    {
        if (deliveryId == Guid.Empty)
        {
            throw new ArgumentException("Delivery identifier is required.", nameof(deliveryId));
        }

        if (searchRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(searchRadiusMeters));
        }

        DeliveryId = deliveryId;
        SearchRadiusMeters = searchRadiusMeters;
        Status = DeliveryMatchingSessionStatus.Active;
        NextAttemptAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid DeliveryId { get; private set; }
    public DeliveryMatchingSessionStatus Status { get; private set; }
    public int SearchRadiusMeters { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? LastAttemptAtUtc { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static DeliveryMatchingSession Create(Guid deliveryId, int initialSearchRadiusMeters, DateTimeOffset utcNow) =>
        new(deliveryId, initialSearchRadiusMeters, utcNow);

    public void RecordAttempt(
        DateTimeOffset utcNow,
        DateTimeOffset nextAttemptAtUtc,
        int nextSearchRadiusMeters)
    {
        if (Status != DeliveryMatchingSessionStatus.Active)
        {
            throw new InvalidOperationException("Only an active matching session can record an attempt.");
        }

        if (nextSearchRadiusMeters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nextSearchRadiusMeters));
        }

        AttemptCount++;
        LastAttemptAtUtc = utcNow.ToUniversalTime();
        NextAttemptAtUtc = nextAttemptAtUtc.ToUniversalTime();
        SearchRadiusMeters = nextSearchRadiusMeters;
    }

    public void Complete(DateTimeOffset utcNow)
    {
        Status = DeliveryMatchingSessionStatus.Assigned;
        CompletedAtUtc = utcNow.ToUniversalTime();
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        Status = DeliveryMatchingSessionStatus.Cancelled;
        CompletedAtUtc = utcNow.ToUniversalTime();
    }
}
