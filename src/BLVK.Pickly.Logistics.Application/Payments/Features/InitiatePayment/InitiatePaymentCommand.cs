using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.InitiatePayment;

using BLVK.Pickly.Logistics.Domain.Common.Auditing;

public sealed record InitiatePaymentCommand(
    decimal Amount,
    Guid CurrencyId,
    PaymentIntent PaymentIntent,
    Guid PaymentProviderId,
    ActorContext ActorContext);
