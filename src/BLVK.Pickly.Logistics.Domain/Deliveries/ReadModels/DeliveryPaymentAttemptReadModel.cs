using BLVK.Pickly.Logistics.Contracts.Payments;

namespace BLVK.Pickly.Logistics.Domain.Deliveries.ReadModels;

public sealed record DeliveryPaymentAttemptReadModel(
    Guid AttemptId,
    Guid DeliveryId,
    Guid PaymentTransactionId,
    string? IdempotencyKey,
    DateTimeOffset? AppliedAtUtc,
    string? ApplicationException,
    Guid PaymentProviderId,
    string PaymentProviderName,
    string PaymentProviderKey,
    string MerchantReference,
    PaymentStatus PaymentStatus,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    decimal? VerifiedAmount,
    Guid? VerifiedCurrencyId,
    string? VerifiedCurrencyCode,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    PaymentMethodType PaymentMethodType,
    Dictionary<string, string> ProviderPayloadMetadata);
