using BLVK.Pickly.Logistics.Domain.Common.Auditing;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;

public sealed record InitiateDeliveryPaymentCommand(
    Guid DeliveryId,
    Guid PaymentProviderId,
    string? IdempotencyKey,
    ActorContext ActorContext);
