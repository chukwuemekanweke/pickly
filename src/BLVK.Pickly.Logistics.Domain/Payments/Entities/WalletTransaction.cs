using BLVK.Pickly.Logistics.Domain.Common.Entities;

namespace BLVK.Pickly.Logistics.Domain.Payments.Entities;

public sealed class WalletTransaction : Entity, IAggregateRoot
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 1000;

    private WalletTransaction()
    {
    }

    private WalletTransaction(
        Guid walletId,
        Guid paymentTransactionId,
        string merchantReference,
        WalletTransactionType transactionType,
        WalletTransactionCategory transactionCategory,
        string transactionTitle,
        string? description,
        decimal amount,
        Guid currencyId)
    {
        if (string.IsNullOrWhiteSpace(merchantReference))
        {
            throw new ArgumentException("Merchant reference is required.", nameof(merchantReference));
        }

        if (string.IsNullOrWhiteSpace(transactionTitle))
        {
            throw new ArgumentException("Transaction title is required.", nameof(transactionTitle));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Wallet transaction amount must be greater than zero.");
        }

        WalletId = walletId;
        PaymentTransactionId = paymentTransactionId;
        MerchantReference = merchantReference.Trim();
        TransactionType = transactionType;
        TransactionCategory = transactionCategory;
        TransactionTitle = Normalize(transactionTitle, MaxTitleLength, nameof(transactionTitle));
        Description = NormalizeOptional(description, MaxDescriptionLength, nameof(description));
        Amount = amount;
        CurrencyId = currencyId;
    }

    public Guid WalletId { get; private set; }
    public Guid PaymentTransactionId { get; private set; }
    public string MerchantReference { get; private set; } = string.Empty;
    public WalletTransactionType TransactionType { get; private set; }
    public WalletTransactionCategory TransactionCategory { get; private set; }
    public string TransactionTitle { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CurrencyId { get; private set; }

    public static WalletTransaction CreateCredit(
        Guid walletId,
        Guid paymentTransactionId,
        string merchantReference,
        decimal amount,
        Guid currencyId,
        WalletTransactionCategory transactionCategory,
        string transactionTitle,
        string? description) =>
        new(
            walletId,
            paymentTransactionId,
            merchantReference,
            WalletTransactionType.Credit,
            transactionCategory,
            transactionTitle,
            description,
            amount,
            currencyId);

    public static WalletTransaction CreateDebit(
        Guid walletId,
        Guid paymentTransactionId,
        string merchantReference,
        decimal amount,
        Guid currencyId,
        WalletTransactionCategory transactionCategory,
        string transactionTitle,
        string? description = null) =>
        new(
            walletId,
            paymentTransactionId,
            merchantReference,
            WalletTransactionType.Debit,
            transactionCategory,
            transactionTitle,
            description,
            amount,
            currencyId);

    private static string Normalize(string value, int maxLength, string argumentName)
    {
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"Value cannot exceed {maxLength} characters.", argumentName);
        }

        return normalized;
    }
}
