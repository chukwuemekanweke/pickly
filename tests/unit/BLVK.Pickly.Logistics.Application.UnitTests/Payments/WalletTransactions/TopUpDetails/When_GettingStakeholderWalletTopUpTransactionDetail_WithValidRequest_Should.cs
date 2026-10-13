using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Payments.ReadModels;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.WalletTransactions.TopUpDetails;

public sealed class When_GettingStakeholderWalletTopUpTransactionDetail_WithValidRequest_Should
{
    [Fact]
    public async Task ReturnTransactionDetail()
    {
        var context = new PaymentsFlowTestContext();
        var stakeholderId = Guid.CreateVersion7();
        var walletTransactionId = Guid.CreateVersion7();
        StakeholderWalletTopUpTransactionDetailRequest? capturedRequest = null;

        context.WalletTransactionReadModelRepository
            .GetWalletTopUpDetailByStakeholderAsync(
                Arg.Do<StakeholderWalletTopUpTransactionDetailRequest>(request => capturedRequest = request),
                Arg.Any<CancellationToken>())
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

        var result = await context.CreateGetStakeholderWalletTopUpTransactionDetailHandler().HandleAsync(
            new GetStakeholderWalletTopUpTransactionDetailCommand(
                walletTransactionId,
                new ActorContext(stakeholderId, Guid.CreateVersion7(), "correlation-id", "flow-id")),
            CancellationToken.None);

        capturedRequest.ShouldNotBeNull();
        capturedRequest.StakeholderId.ShouldBe(stakeholderId);
        capturedRequest.WalletTransactionId.ShouldBe(walletTransactionId);
        result.Status.ShouldBe(GetStakeholderWalletTopUpTransactionDetailStatus.Success);
        result.Transaction.ShouldNotBeNull();
        result.Transaction.WalletTransactionId.ShouldBe(walletTransactionId);
        result.Transaction.PaymentMethodType.ShouldBe("BankTransfer");
        result.Transaction.PaymentProviderName.ShouldBe("SafeHaven");
    }
}
