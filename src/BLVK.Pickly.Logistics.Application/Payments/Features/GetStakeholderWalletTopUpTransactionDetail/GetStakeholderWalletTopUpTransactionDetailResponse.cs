namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;

public sealed record GetStakeholderWalletTopUpTransactionDetailResponse(
    Guid WalletTransactionId,
    string TransactionTitle,
    string? Description,
    string MerchantReference,
    decimal Amount,
    string CurrencyCode,
    string PaymentMethodType,
    string PaymentProviderName,
    DateTimeOffset Timestamp);