namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;

public sealed record CreateDeliveryResult(
    CreateDeliveryStatus Status,
    DeliveryResponse? Delivery = null,
    bool IsReplay = false);
