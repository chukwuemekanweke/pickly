using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;

public sealed class GetStakeholderWalletTopUpTransactionDetailHandler(
    IWalletTransactionReadModelRepository walletTransactionReadModelRepository)
{
    public async Task<GetStakeholderWalletTopUpTransactionDetailResult> HandleAsync(
        GetStakeholderWalletTopUpTransactionDetailCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to retrieve wallet top-up transaction details.");

        var transaction = await walletTransactionReadModelRepository.GetWalletTopUpDetailByStakeholderAsync(
            new StakeholderWalletTopUpTransactionDetailRequest(stakeholderId, command.WalletTransactionId),
            cancellationToken);

        if (transaction is null)
        {
            return new GetStakeholderWalletTopUpTransactionDetailResult(
                GetStakeholderWalletTopUpTransactionDetailStatus.NotFound,
                Error: $"Wallet top-up transaction '{command.WalletTransactionId}' was not found.");
        }

        return new GetStakeholderWalletTopUpTransactionDetailResult(
            GetStakeholderWalletTopUpTransactionDetailStatus.Success,
            new GetStakeholderWalletTopUpTransactionDetailResponse(
                transaction.WalletTransactionId,
                transaction.TransactionTitle,
                transaction.Description,
                transaction.MerchantReference,
                transaction.Amount,
                transaction.CurrencyCode,
                transaction.PaymentMethodType.ToString(),
                transaction.PaymentProviderName,
                transaction.Timestamp));
    }
}