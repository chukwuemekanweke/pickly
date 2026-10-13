using BLVK.Pickly.Logistics.Domain.Payments.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTransactionReadModel(
    Guid WalletTransactionId,
    string TransactionTitle,
    decimal Amount,
    string CurrencyCode,
    WalletTransactionType TransactionType,
    WalletTransactionCategory TransactionCategory,
    DateTimeOffset Timestamp);
