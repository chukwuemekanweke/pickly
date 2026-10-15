namespace BLVK.Pickly.Logistics.Domain.Deliveries.Enums;

public enum DeliveryStatus
{
    AwaitingPayment = 1,
    AwaitingDispatch = 2,
    PendingAssignment = 3,
    RiderAssigned = 4,
    PickedUp = 5,
    InTransit = 6,
    Delivered = 7,
    DeliveryFailed = 8,
    Cancelled = 9
}
