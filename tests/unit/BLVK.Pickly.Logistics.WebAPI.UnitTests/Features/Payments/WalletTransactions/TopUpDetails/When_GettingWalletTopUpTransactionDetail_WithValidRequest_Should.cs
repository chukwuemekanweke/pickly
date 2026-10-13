using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.WalletTransactions;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Payments.WalletTransactions.TopUpDetails;

public sealed class When_GettingWalletTopUpTransactionDetail_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnTransactionDetail()
    {
        var context = new PaymentsControllerTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var walletTransactionId = Guid.CreateVersion7();

        context.CurrentActor.ActorId.Returns(stakeholderId.ToString());
        context.CurrentActor.TenantId.Returns(Guid.CreateVersion7());
        context.CurrentActor.CorrelationId.Returns("correlation-id");
        context.CurrentActor.FlowId.Returns("flow-id");
        context.WalletTransactionReadModelRepository
            .GetWalletTopUpDetailByStakeholderAsync(Arg.Any<StakeholderWalletTopUpTransactionDetailRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StakeholderWalletTopUpTransactionDetailReadModel(
                walletTransactionId,
                "Wallet funding",
                "Wallet funded via bank transfer.",
                "merchant-ref-123",
                2500m,
                "NGN",
                BLVK.Pickly.Logistics.Contracts.Payments.PaymentMethodType.BankTransfer,
                "SafeHaven",
                context.Clock.GetUtcNow()));

        var sut = new WalletTransactionsController(
            context.CreateGetStakeholderWalletTransactionsHandler(),
            new GetStakeholderWalletTransactionsValidator(),
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler(),
            new GetStakeholderWalletTopUpTransactionDetailValidator(),
            context.CurrentActor);

        var result = await sut.GetTopUpDetail(new GetStakeholderWalletTopUpTransactionDetailRequest(walletTransactionId), CancellationToken.None);

        var ok = result.Result.ShouldBeOfType<OkObjectResult>();
        var payload = ok.Value.ShouldBeOfType<GetStakeholderWalletTopUpTransactionDetailResponse>();
        payload.WalletTransactionId.ShouldBe(walletTransactionId);
        payload.PaymentMethodType.ShouldBe("BankTransfer");
        payload.PaymentProviderName.ShouldBe("SafeHaven");
    }
}
