using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;

public enum InitiateDeliveryPaymentStatus
{
    Success,
    NotAuthenticated,
    NotFound,
    NotPayable,
    AlreadyPaid,
    ProviderUnavailable,
    ProviderConflict
}

public sealed record InitiateDeliveryPaymentResult(
    InitiateDeliveryPaymentStatus Status,
    Guid? PaymentTransactionId = null,
    string? MerchantReference = null,
    PaymentStatus? PaymentStatus = null,
    Guid? PaymentProviderId = null,
    string? PaymentProviderName = null,
    DateTimeOffset? ExpiresAtUtc = null,
    PaymentMethodType? PaymentMethodType = null,
    IReadOnlyDictionary<string, string>? PaymentInstruction = null,
    bool IsReplay = false);
