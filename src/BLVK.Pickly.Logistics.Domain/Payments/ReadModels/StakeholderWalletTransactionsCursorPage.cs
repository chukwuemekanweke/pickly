namespace BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTransactionsCursorPage(
    IReadOnlyList<StakeholderWalletTransactionReadModel> Transactions,
    bool HasMore);
