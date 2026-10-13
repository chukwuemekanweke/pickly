namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.InitiatePayment;

public sealed record InitiatePaymentRequest(
    decimal Amount,
    Guid CurrencyId,
    string PaymentIntent,
    Guid PaymentProviderId);
