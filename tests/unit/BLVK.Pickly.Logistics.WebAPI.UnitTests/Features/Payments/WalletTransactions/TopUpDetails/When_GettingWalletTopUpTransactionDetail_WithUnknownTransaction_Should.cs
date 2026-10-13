using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.WalletTransactions.TopUpDetails;

public sealed class When_GettingWalletTopUpTransactionDetail_WithUnknownTransaction_Should
{
    [Fact]
    public async Task ReturnNotFound()
    {
        var context = new PaymentsControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();

        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        context.CurrentActor.TenantId.Returns(Guid.CreateVersion7());
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");
        context.WalletTransactionReadModelRepository
            .GetWalletTopUpDetailByStakeholderAsync(Arg.Any<StakeholderWalletTopUpTransactionDetailRequest>(), Arg.Any<CancellationToken>())
            .Returns((StakeholderWalletTopUpTransactionDetailReadModel?)null);

        var sut = new WalletTransactionsController(
            context.CreateGetStakeholderWalletTransactionsHandler(),
            new GetStakeholderWalletTransactionsValidator(),
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler(),
            new GetStakeholderWalletTopUpTransactionDetailValidator(),
            context.CurrentActor);

        var result = await sut.GetTopUpDetail(new GetStakeholderWalletTopUpTransactionDetailRequest(Guid.CreateVersion7()), CancellationToken.None);

        result.Result.ShouldBeOfType<NotFoundObjectResult>();
    }
}
