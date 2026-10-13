namespace BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

public sealed record StakeholderWalletTopUpTransactionDetailRequest(
    Guid StakeholderId,
    Guid WalletTransactionId);
