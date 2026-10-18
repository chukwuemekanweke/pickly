using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.InitiateDeliveryPayment;

public enum InitiateDeliveryPaymentStatus
{
    Success = 1,
    NotAuthenticated = 2,
    NotFound = 3,
    NotPayable = 4,
    AlreadyPaid = 5,
    ProviderUnavailable = 6,
    ProviderConflict = 7,
    PaymentRequiresReview = 8,
    PaymentFailed = 9,
    PaymentExpired = 10,
    PaymentCancelled = 11
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
