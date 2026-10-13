namespace BLVK.Pickly.Logistics.WebAPI.Features.Payments.InitiatePayment;

public sealed record InitiatePaymentResponse(
    string MerchantReference,
    string PaymentStatus,
    Guid PaymentProviderId,
    string PaymentProviderName,
    DateTimeOffset? ExpiresAtUtc,
    string PaymentMethodType,
    IReadOnlyDictionary<string, string> PaymentInstruction);
