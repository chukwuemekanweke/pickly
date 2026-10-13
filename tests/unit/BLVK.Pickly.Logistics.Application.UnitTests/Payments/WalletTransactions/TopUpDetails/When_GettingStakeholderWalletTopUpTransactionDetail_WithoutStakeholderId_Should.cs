using BLVK.Pickly.Logistics.Application.Payments.Features.GetStakeholderWalletTopUpTransactionDetail;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Payments.WalletTransactions.TopUpDetails;

public sealed class When_GettingStakeholderWalletTopUpTransactionDetail_WithoutStakeholderId_Should
{
    [Fact]
    public async Task ThrowInvalidOperationException()
    {
        var context = new PaymentsFlowTestContext();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
            context.CreateGetStakeholderWalletTopUpTransactionDetailHandler().HandleAsync(
                new GetStakeholderWalletTopUpTransactionDetailCommand(
                    Guid.CreateVersion7(),
                    new ActorContext(null, Guid.CreateVersion7(), "correlation-id", "flow-id")),
                CancellationToken.None));

        exception.Message.ShouldBe("Authenticated stakeholder id is required to retrieve wallet top-up transaction details.");
    }
}
