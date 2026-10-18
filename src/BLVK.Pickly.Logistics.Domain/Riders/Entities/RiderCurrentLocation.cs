using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Entities;

public sealed class RiderCurrentLocation : Entity, IAggregateRoot
{
    private RiderCurrentLocation()
    {
    }

    private RiderCurrentLocation(Guid riderProfileId, DateTimeOffset capturedAtUtc, DateTimeOffset receivedAtUtc)
    {
        if (riderProfileId == Guid.Empty)
        {
            throw new ArgumentException("Rider profile identifier is required.", nameof(riderProfileId));
        }

        RiderProfileId = riderProfileId;
        CapturedAtUtc = capturedAtUtc.ToUniversalTime();
        ReceivedAtUtc = receivedAtUtc.ToUniversalTime();
    }

    public Guid RiderProfileId { get; private set; }
    public DateTimeOffset CapturedAtUtc { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public static RiderCurrentLocation Create(
        Guid riderProfileId,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset receivedAtUtc) =>
        new(riderProfileId, capturedAtUtc, receivedAtUtc);

    public bool TryAdvance(DateTimeOffset capturedAtUtc, DateTimeOffset receivedAtUtc)
    {
        var captured = capturedAtUtc.ToUniversalTime();
        if (captured <= CapturedAtUtc)
        {
            return false;
        }

        CapturedAtUtc = captured;
        ReceivedAtUtc = receivedAtUtc.ToUniversalTime();
        return true;
    }
}
