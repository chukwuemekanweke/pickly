using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTransactions;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.WalletTransactions;

public sealed class When_GettingWalletTransactions_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnPaginatedTransactions()
    {
        var context = new PaymentsControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();

        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        context.CurrentActor.TenantId.Returns(Guid.CreateVersion7());
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");
        context.WalletTransactionReadModelRepository
            .GetByStakeholderAsync(Arg.Any<StakeholderWalletTransactionsCursorRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StakeholderWalletTransactionsCursorPage(
                [
                    new StakeholderWalletTransactionReadModel(
                        Guid.CreateVersion7(),
                        WalletTransactionTitles.WalletFunding,
                        2500m,
                        "NGN",
                        WalletTransactionType.Credit,
                        WalletTransactionCategory.WalletFunding,
                        context.Clock.GetUtcNow())
                ],
                false));

        var sut = new WalletTransactionsController(
            context.CreateGetStakeholderWalletTransactionsHandler(),
            new GetStakeholderWalletTransactionsValidator(),
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler(),
            new GetStakeholderWalletTopUpTransactionDetailValidator(),
            context.CurrentActor);

        var result = await sut.Handle(new GetStakeholderWalletTransactionsRequest(20, null), CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GetStakeholderWalletTransactionsResult>();
        payload.Transactions.Count.ShouldBe(1);
        payload.Transactions[0].TransactionTitle.ShouldBe(WalletTransactionTitles.WalletFunding);
        payload.NextCursor.ShouldBeNull();
    }
}
