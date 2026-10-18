namespace BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDeliveryPayments;

public enum GetDeliveryPaymentsStatus
{
    Success,
    NotAuthenticated,
    NotFound
}

public sealed record DeliveryPaymentAttemptResponse(
    Guid PaymentTransactionId,
    Guid PaymentProviderId,
    string PaymentProviderName,
    string MerchantReference,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyDictionary<string, string> PaymentInstruction);

public sealed record DeliveryPaymentsResponse(
    Guid DeliveryId,
    string PaymentStatus,
    bool PaymentRequired,
    decimal Amount,
    Guid CurrencyId,
    string CurrencyCode,
    IReadOnlyList<DeliveryPaymentAttemptResponse> Attempts);

public sealed record GetDeliveryPaymentsResult(
    GetDeliveryPaymentsStatus Status,
    DeliveryPaymentsResponse? Response = null);
