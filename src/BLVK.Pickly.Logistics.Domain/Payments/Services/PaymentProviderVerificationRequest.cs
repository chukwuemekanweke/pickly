using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Domain.Payments.Services;

public sealed record PaymentProviderVerificationRequest(
    string MerchantReference,
    string? ProviderReference,
    decimal Amount,
    string CurrencyCode,
    PaymentIntent PaymentIntent);
