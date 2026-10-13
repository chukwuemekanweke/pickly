using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;
using NSubstitute;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests;

public sealed class When_VerifyingSafeHavenVirtualAccount_WithExpiredStatus_Should
{
    [Fact]
    public async Task ReturnExpired()
    {
        var client = Substitute.For<ISafeHavenClient>();
        var sut = new SafeHavenPaymentProviderService(
            client,
            TimeProvider.System);

        client.GetVirtualAccountAsync("provider-ref", Arg.Any<CancellationToken>())
            .Returns(new SafeHavenResponse<SafeHavenVirtualAccount>(
                200,
                "ok",
                new SafeHavenVirtualAccount
                {
                    Id = "provider-ref",
                    Status = SafeHavenVirtualAccountStatuses.Expired
                }));

        var result = await sut.VerifyPaymentAsync(
            new PaymentProviderVerificationRequest(
                "merchant-ref",
                "provider-ref",
                1500m,
                "NGN",
                PaymentIntent.WalletTopUp),
            CancellationToken.None);

        result.VerificationStatus.ShouldBe(PaymentProviderVerificationStatus.Expired);
        result.ProviderReference.ShouldBe("provider-ref");
        result.FailureReason.ShouldBe("provider_reported_expired");
        result.StatusChangeReason.ShouldBe(KnownPaymentTransactionChangeReasons.ReconciliationConfirmedExpired);
    }
}
