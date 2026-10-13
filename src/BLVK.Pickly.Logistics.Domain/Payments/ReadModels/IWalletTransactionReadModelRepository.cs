namespace BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

public interface IWalletTransactionReadModelRepository
{
    Task<StakeholderWalletTransactionsCursorPage> GetByStakeholderAsync(
        StakeholderWalletTransactionsCursorRequest request,
        CancellationToken cancellationToken);

    Task<StakeholderWalletTopUpTransactionDetailReadModel?> GetWalletTopUpDetailByStakeholderAsync(
        StakeholderWalletTopUpTransactionDetailRequest request,
        CancellationToken cancellationToken);
}
