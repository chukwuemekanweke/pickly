using BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.WalletTransactions.TopUpDetails;

public sealed class When_GettingWalletTopUpTransactionDetail_WithInvalidRequest_Should
{
    [Fact]
    public async Task ReturnBadRequest()
    {
        var context = new PaymentsControllerTestContext();
        var sut = new WalletTransactionsController(
            context.CreateGetStakeholderWalletTransactionsHandler(),
            new GetStakeholderWalletTransactionsValidator(),
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler(),
            new GetStakeholderWalletTopUpTransactionDetailValidator(),
            context.CurrentActor);

        var result = await sut.GetTopUpDetail(new GetStakeholderWalletTopUpTransactionDetailRequest(Guid.Empty), CancellationToken.None);

        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }
}
