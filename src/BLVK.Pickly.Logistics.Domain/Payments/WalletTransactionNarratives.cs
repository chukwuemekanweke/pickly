namespace BLVK.Pickly.Logistics.Domain.Payments;

public static class WalletTransactionNarratives
{
    public static readonly WalletTransactionNarrativeTemplate WalletFunding =
        new(WalletTransactionTitles.WalletFunding, "Wallet funded via bank transfer.");

    public static readonly WalletTransactionNarrativeTemplate BankTransferCredit =
        new(WalletTransactionTitles.BankTransferCredit, "Bank transfer received.");
}
