using BLVK.Pickly.Logistics.Application.Common.Pagination;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;

namespace BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;

public sealed class GetStakeholderWalletTransactionsHandler(IWalletTransactionReadModelRepository walletTransactionReadModelRepository)
{
    public async Task<GetStakeholderWalletTransactionsResult> HandleAsync(
        GetStakeholderWalletTransactionsCommand command,
        CancellationToken cancellationToken)
    {
        var stakeholderId = command.ActorContext.StakeholderId
            ?? throw new InvalidOperationException("Authenticated stakeholder id is required to retrieve wallet transactions.");

        var (cursorCreatedAtUtc, cursorTransactionId) = CursorPagination.Decode(command.Cursor);

        var page = await walletTransactionReadModelRepository.GetByStakeholderAsync(
            new StakeholderWalletTransactionsCursorRequest(
                stakeholderId,
                cursorCreatedAtUtc,
                cursorTransactionId,
                command.Limit),
            cancellationToken);

        var transactions = page.Transactions
            .Select(transaction => new GetStakeholderWalletTransactionsResponse(
                transaction.TransactionTitle,
                transaction.Amount,
                transaction.CurrencyCode,
                transaction.TransactionType.ToString(),
                transaction.TransactionCategory.ToString(),
                transaction.Timestamp))
            .ToArray();

        var nextCursor = page.HasMore
            ? CursorPagination.Encode(
                page.Transactions[^1].Timestamp,
                page.Transactions[^1].WalletTransactionId)
            : null;

        return new GetStakeholderWalletTransactionsResult(transactions, nextCursor);
    }
}
