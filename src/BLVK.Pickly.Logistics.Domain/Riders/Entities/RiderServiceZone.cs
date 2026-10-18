using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Riders.Entities;

public sealed class RiderServiceZone : Entity, IAggregateRoot
{
    private RiderServiceZone()
    {
    }

    private RiderServiceZone(Guid riderProfileId, Guid deliveryZoneId)
    {
        if (riderProfileId == Guid.Empty)
        {
            throw new ArgumentException("Rider profile identifier is required.", nameof(riderProfileId));
        }

        if (deliveryZoneId == Guid.Empty)
        {
            throw new ArgumentException("Delivery zone identifier is required.", nameof(deliveryZoneId));
        }

        RiderProfileId = riderProfileId;
        DeliveryZoneId = deliveryZoneId;
    }

    public Guid RiderProfileId { get; private set; }
    public Guid DeliveryZoneId { get; private set; }

    public static RiderServiceZone Create(Guid riderProfileId, Guid deliveryZoneId) =>
        new(riderProfileId, deliveryZoneId);
}
