using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.InitiatePayment;

public sealed record InitiatePaymentResult(
    string MerchantReference,
    PaymentStatus PaymentStatus,
    Guid PaymentProviderId,
    string PaymentProviderName,
    DateTimeOffset? ExpiresAtUtc,
    PaymentMethodType PaymentMethodType,
    IReadOnlyDictionary<string, string> InstructionFields);
