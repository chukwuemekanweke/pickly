using BLVK.Pickly.Logistics.Domain.Common.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.Riders.Entities;

/// <summary>
/// Append-only record of a persisted <see cref="RiderProfile.AvailabilityStatus"/> change, used to reconstruct
/// rider availability at a past point in time.
/// </summary>
public sealed class RiderAvailabilityHistory : Entity
{
    private RiderAvailabilityHistory()
    {
    }

    private RiderAvailabilityHistory(
        Guid riderProfileId,
        RiderAvailabilityStatus? previousStatus,
        RiderAvailabilityStatus newStatus,
        DateTimeOffset occurredAtUtc)
    {
        if (riderProfileId == Guid.Empty)
        {
            throw new ArgumentException("A non-empty identifier is required.", nameof(riderProfileId));
        }

        RiderProfileId = riderProfileId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid RiderProfileId { get; private set; }
    public RiderAvailabilityStatus? PreviousStatus { get; private set; }
    public RiderAvailabilityStatus NewStatus { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public static RiderAvailabilityHistory Create(
        Guid riderProfileId,
        RiderAvailabilityStatus? previousStatus,
        RiderAvailabilityStatus newStatus,
        DateTimeOffset occurredAtUtc) =>
        new(riderProfileId, previousStatus, newStatus, occurredAtUtc);
}
