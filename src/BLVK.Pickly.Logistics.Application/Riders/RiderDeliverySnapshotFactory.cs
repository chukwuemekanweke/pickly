using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;

namespace BLVK.Pickly.Logistics.Application.Riders;

internal static class RiderDeliverySnapshotFactory
{
    public static RiderAssignedDeliverySnapshot Create(Delivery delivery) =>
        new(
            delivery.Id,
            delivery.TrackingId,
            delivery.Status.ToString(),
            delivery.DeliveryType.ToString(),
            delivery.OrderType.ToString(),
            delivery.PaymentMethod.ToString(),
            delivery.PaymentResponsibleParty.ToString(),
            delivery.PickupAddress.AddressLine,
            delivery.PickupAddress.Latitude,
            delivery.PickupAddress.Longitude,
            delivery.DropOffAddress.AddressLine,
            delivery.DropOffAddress.Latitude,
            delivery.DropOffAddress.Longitude,
            delivery.Recipient.Name,
            delivery.Recipient.PhoneNumber,
            delivery.Items.Select(item => new RiderDeliveryItemSnapshot(item.Description, item.Quantity, item.WeightKg)).ToArray());
}
