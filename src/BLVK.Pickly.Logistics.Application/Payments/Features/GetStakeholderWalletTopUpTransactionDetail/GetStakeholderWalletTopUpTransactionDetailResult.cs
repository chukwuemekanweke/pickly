namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;

public sealed record GetStakeholderWalletTopUpTransactionDetailResult(
    GetStakeholderWalletTopUpTransactionDetailStatus Status,
    GetStakeholderWalletTopUpTransactionDetailResponse? Transaction = null,
    string? Error = null);
