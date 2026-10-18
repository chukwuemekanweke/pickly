using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;

namespace BLVK.Pickly.Logistics.Domain.UnitTests.Riders;

public sealed class When_ManagingRiderOffer_Should
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PreventAcceptanceAtOrAfterExpiry()
    {
        var offer = CreateOffer();

        Should.Throw<AggregateStateException>(() => offer.Accept(Now.AddSeconds(30)));
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Pending);
    }

    [Fact]
    public void MakeRepeatedRejectionIdempotent()
    {
        var offer = CreateOffer();

        offer.Reject(Now.AddSeconds(1));
        offer.Reject(Now.AddSeconds(2));

        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Rejected);
        offer.RespondedAtUtc.ShouldBe(Now.AddSeconds(1));
    }

    [Fact]
    public void AcceptOnlyOnce()
    {
        var offer = CreateOffer();
        offer.Accept(Now.AddSeconds(1));

        Should.Throw<AggregateStateException>(() => offer.Accept(Now.AddSeconds(2)));
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Accepted);
    }

    private static RiderDeliveryOffer CreateOffer() =>
        RiderDeliveryOffer.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Now,
            Now.AddSeconds(30));
}
