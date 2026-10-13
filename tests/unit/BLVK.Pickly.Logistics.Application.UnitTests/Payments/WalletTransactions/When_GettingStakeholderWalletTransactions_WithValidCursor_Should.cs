using BLVK.Pickly.Logistics.Application.Common.Pagination;
using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.WalletTransactions;

public sealed class When_GettingStakeholderWalletTransactions_WithValidCursor_Should
{
    [Fact]
    public async Task ReturnCursorPaginatedTransactions()
    {
        var context = new PaymentsFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var cursorTransactionId = Guid.CreateVersion7();
        var cursorCreatedAtUtc = context.Clock.GetUtcNow().AddMinutes(-5);
        StakeholderWalletTransactionsCursorRequest? capturedRequest = null;

        context.WalletTransactionReadModelRepository
            .GetByStakeholderAsync(
                Arg.Do<StakeholderWalletTransactionsCursorRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
            .Returns(new StakeholderWalletTransactionsCursorPage(
                [
                    new StakeholderWalletTransactionReadModel(
                        Guid.CreateVersion7(),
                        WalletTransactionTitles.WalletFunding,
                        2500m,
                        "NGN",
                        WalletTransactionType.Credit,
                        WalletTransactionCategory.WalletFunding,
                        context.Clock.GetUtcNow().AddMinutes(-1)),
                    new StakeholderWalletTransactionReadModel(
                        Guid.CreateVersion7(),
                        WalletTransactionTitles.BankTransferCredit,
                        1200m,
                        "NGN",
                        WalletTransactionType.Credit,
                        WalletTransactionCategory.BankTransferCredit,
                        context.Clock.GetUtcNow().AddMinutes(-2))
                ],
                true));

        var cursor = CursorPagination.Encode(cursorCreatedAtUtc, cursorTransactionId);

        var result = await context.CreateGetStakeholderWalletTransactionsHandler().HandleAsync(
            new GetStakeholderWalletTransactionsCommand(
                2,
                cursor,
                new ActorContext(stakeholderId, Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
        capturedRequest.CursorCreatedAtUtc.ShouldBe(cursorCreatedAtUtc);
        capturedRequest.CursorTransactionId.ShouldBe(cursorTransactionId);
        capturedRequest.Limit.ShouldBe(2);
        result.Transactions.Count.ShouldBe(2);
        result.Transactions[0].TransactionTitle.ShouldBe(WalletTransactionTitles.WalletFunding);
        result.Transactions[0].TransactionType.ShouldBe(nameof(WalletTransactionType.Credit));
        result.Transactions[0].TransactionCategory.ShouldBe(nameof(WalletTransactionCategory.WalletFunding));
        result.NextCursor.ShouldNotBeNull();
    }
}
