namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDelivery;

public sealed record GetDeliveryResult(GetDeliveryStatus Status, DeliveryResponse? Delivery = null);

public enum GetDeliveryStatus
{
    Success,
    NotAuthenticated,
    NotFound
}
