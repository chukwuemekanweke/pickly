namespace BLVK.Pickly.Logistics.Domain.Riders.Operations;

public sealed record RiderAssignedDeliverySnapshot(
    Guid DeliveryId,
    string TrackingId,
    string Status,
    string DeliveryType,
    string OrderType,
    string PaymentMethod,
    string PaymentResponsibleParty,
    string PickupAddress,
    decimal PickupLatitude,
    decimal PickupLongitude,
    string DropOffAddress,
    decimal DropOffLatitude,
    decimal DropOffLongitude,
    string RecipientName,
    string RecipientPhoneNumber,
    IReadOnlyCollection<RiderDeliveryItemSnapshot> Items);

public sealed record RiderDeliveryItemSnapshot(string Description, int Quantity, decimal WeightKg);
