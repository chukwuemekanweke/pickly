using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_VerifyingCredoTransaction_WithRefundStatus_Should
{
    [Fact]
    public async Task ReturnFailedWithRefundQueuedReason()
    {
        var client = Substitute.For<ICredoClient>();
        var stakeholderReadModelRepository = Substitute.For<IStakeholderReadModelRepository>();
        var sut = new CredoPaymentProviderService(client, stakeholderReadModelRepository);

        client.VerifyTransactionAsync("merchant-ref", Arg.Any<CancellationToken>())
            .Returns(new CredoVerifyTransactionResponse(
                "Queued for refund",
                "business-code",
                "provider-ref",
                "merchant-ref",
                1500m,
                1500m,
                0m,
                1500m,
                "customer@example.com",
                "2026-05-02 00:00:00",
                0,
                "NGN",
                CredoTransactionStatuses.Refund,
                []));

        var result = await sut.VerifyPaymentAsync(
            new PaymentProviderVerificationRequest(
                "merchant-ref",
                "provider-ref",
                1500m,
                "NGN",
                PaymentIntent.WalletTopUp),
            CancellationToken.None);

        result.VerificationStatus.ShouldBe(PaymentProviderVerificationStatus.Failed);
        result.FailureReason.ShouldBe("Queued for refund");
        result.StatusChangeReason.ShouldBe(KnownPaymentTransactionChangeReasons.ReconciliationConfirmedRefundQueued);
    }
}
