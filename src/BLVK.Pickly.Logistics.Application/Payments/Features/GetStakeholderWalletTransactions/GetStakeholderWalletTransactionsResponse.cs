namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed record GetStakeholderWalletTransactionsResponse(
    string TransactionTitle,
    decimal Amount,
    string CurrencyCode,
    string TransactionType,
    string TransactionCategory,
    DateTimeOffset Timestamp);
