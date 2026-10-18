using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Deliveries.Bookings;

public sealed class When_CreatingDelivery_FromValidQuote_Should
{
    public static TheoryData<DeliveryType, DeliveryOrderType, DeliveryStatus> Combinations => new()
    {
        { DeliveryType.Instant, DeliveryOrderType.PayBeforeDelivery, DeliveryStatus.AwaitingPayment },
        { DeliveryType.Instant, DeliveryOrderType.PayOnDelivery, DeliveryStatus.PendingAssignment },
        { DeliveryType.Scheduled, DeliveryOrderType.PayBeforeDelivery, DeliveryStatus.AwaitingPayment },
        { DeliveryType.Scheduled, DeliveryOrderType.PayOnDelivery, DeliveryStatus.AwaitingDispatch }
    };

    [Theory]
    [MemberData(nameof(Combinations))]
    public async Task CopyAuthoritativeSnapshotsAndInitialState(
        DeliveryType deliveryType,
        DeliveryOrderType orderType,
        DeliveryStatus expectedStatus)
    {
        var context = new TestContext(deliveryType, orderType);

        var result = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(CreateDeliveryStatus.Success);
        result.IsReplay.ShouldBeFalse();
        result.Delivery.ShouldNotBeNull();
        result.Delivery.QuoteId.ShouldBe(context.Quote.Id);
        result.Delivery.VehicleCategoryId.ShouldBe(context.Quote.VehicleCategoryId);
        result.Delivery.Status.ShouldBe(expectedStatus.ToString());
        result.Delivery.PaymentStatus.ShouldBe(DeliveryPaymentStatus.Pending.ToString());
        result.Delivery.Charge.Amount.ShouldBe(context.Quote.TotalAmount);
        result.Delivery.Charge.CurrencyCode.ShouldBe(context.Currency.CurrencyCode);
        result.Delivery.Items.Single().WeightKg.ShouldBe(2.5m);
        result.Delivery.PickupAddress.AdditionalInformation.ShouldBe("Warehouse gate");
        result.Delivery.DropOffAddress.AdditionalInformation.ShouldBe("Blue gate");
        result.Delivery.TrackingId.ShouldBe("PKL-ABCDEFGH");
        context.Quote.ConsumedByDeliveryId.ShouldBe(result.Delivery.DeliveryId);

        if (deliveryType == DeliveryType.Instant)
        {
            result.Delivery.ScheduledPickupAtUtc.ShouldBeNull();
            result.Delivery.DispatchEligibleAtUtc.ShouldBeNull();
        }
        else
        {
            result.Delivery.DispatchEligibleAtUtc.ShouldBe(TestContext.Now.AddMinutes(10));
        }
    }

    [Fact]
    public async Task ReturnOriginalDeliveryForRetryAfterQuoteExpiry()
    {
        var context = new TestContext(DeliveryType.Instant, DeliveryOrderType.PayOnDelivery);
        var first = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);
        context.Advance(TimeSpan.FromMinutes(20));

        var replay = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        replay.Status.ShouldBe(CreateDeliveryStatus.Success);
        replay.IsReplay.ShouldBeTrue();
        replay.Delivery!.DeliveryId.ShouldBe(first.Delivery!.DeliveryId);
        await context.Persistence.Received(1).CommitAsync(
            Arg.Any<Delivery>(),
            Arg.Any<DeliveryQuote>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnOriginalDeliveryUnchangedForRetryWithDifferentPayload()
    {
        var context = new TestContext(DeliveryType.Instant, DeliveryOrderType.PayOnDelivery);
        var first = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        var replay = await context.Handler.HandleAsync(
            context.Command("Different Recipient", "+2348099999999", "Different gate"),
            CancellationToken.None);

        replay.Status.ShouldBe(CreateDeliveryStatus.Success);
        replay.IsReplay.ShouldBeTrue();
        replay.Delivery!.DeliveryId.ShouldBe(first.Delivery!.DeliveryId);
        replay.Delivery.Recipient.Name.ShouldBe("Ada Recipient");
        replay.Delivery.Recipient.PhoneNumber.ShouldBe("+2348012345678");
        replay.Delivery.DropOffAddress.AdditionalInformation.ShouldBe("Blue gate");
        await context.Persistence.Received(1).CommitAsync(
            Arg.Any<Delivery>(),
            Arg.Any<DeliveryQuote>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HideUnknownOrCrossSenderQuote()
    {
        var context = new TestContext(DeliveryType.Instant, DeliveryOrderType.PayOnDelivery);
        context.HideQuote();

        var result = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(CreateDeliveryStatus.NotFound);
    }

    [Fact]
    public async Task RejectExpiredUnusedQuoteBeforeCreatingAnything()
    {
        var context = new TestContext(
            DeliveryType.Instant,
            DeliveryOrderType.PayOnDelivery,
            expiresAtUtc: TestContext.Now);

        var result = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(CreateDeliveryStatus.QuoteExpired);
        await context.Persistence.DidNotReceive().CommitAsync(
            Arg.Any<Delivery>(),
            Arg.Any<DeliveryQuote>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectConsumedQuoteWithoutExistingDelivery()
    {
        var context = new TestContext(DeliveryType.Instant, DeliveryOrderType.PayOnDelivery);
        context.Quote.Consume(Guid.CreateVersion7(), TestContext.Now);

        var result = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(CreateDeliveryStatus.QuoteConsumed);
    }

    [Fact]
    public async Task RejectScheduledPickupThatIsNoLongerFuture()
    {
        var context = new TestContext(
            DeliveryType.Scheduled,
            DeliveryOrderType.PayOnDelivery,
            scheduledPickupAtUtc: TestContext.Now.AddSeconds(-1));

        var result = await context.Handler.HandleAsync(context.Command(), CancellationToken.None);

        result.Status.ShouldBe(CreateDeliveryStatus.ScheduledPickupExpired);
    }

    private sealed class TestContext
    {
        public static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        private readonly IRepository<DeliveryQuote> _quoteRepository;
        private readonly MutableTimeProvider _timeProvider = new(Now);
        private readonly Guid _senderId = Guid.CreateVersion7();
        private Delivery? _createdDelivery;

        public TestContext(
            DeliveryType deliveryType,
            DeliveryOrderType orderType,
            DateTimeOffset? expiresAtUtc = null,
            DateTimeOffset? scheduledPickupAtUtc = null)
        {
            Currency = Currency.Create("NGN", "Naira", true);
            var vehicle = VehicleCategory.Create("BIKE", "Bike", null, 50m, "driving");
            var zone = DeliveryZone.Create("ENUGU", "Enugu", "NG", 1, Now.AddDays(-1));
            var policy = DeliveryPricingPolicy.Create("v1", Currency.Id, 100m, 1m, 1m, Now.AddDays(-1));
            Quote = DeliveryQuote.Create(
                _senderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Address("Pickup", "Warehouse gate"),
                Address("Dropoff", null),
                deliveryType,
                orderType,
                deliveryType == DeliveryType.Scheduled
                    ? scheduledPickupAtUtc ?? Now.AddMinutes(40)
                    : null,
                vehicle,
                new(zone.Id, zone.Code, zone.Id, zone.Code),
                policy,
                new(100m, 20m, 10m, 5m, 5m),
                [("Parcel", 2, 2.5m)],
                Now.AddMinutes(-1),
                expiresAtUtc ?? Now.AddMinutes(15));

            _quoteRepository = Substitute.For<IRepository<DeliveryQuote>>();
            _quoteRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<DeliveryQuote>>(), Arg.Any<CancellationToken>())
                .Returns(Quote);
            var deliveryRepository = Substitute.For<IRepository<Delivery>>();
            deliveryRepository.FirstOrDefaultAsync(Arg.Any<ISpecification<Delivery>>(), Arg.Any<CancellationToken>())
                .Returns(_ => _createdDelivery);
            var currencyRepository = Substitute.For<IRepository<Currency>>();
            currencyRepository.GetByIdAsync(Currency.Id, Arg.Any<CancellationToken>()).Returns(Currency);
            Persistence = Substitute.For<IDeliveryBookingPersistence>();
            Persistence.CommitAsync(
                    Arg.Any<Delivery>(),
                    Arg.Any<DeliveryQuote>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    _createdDelivery = callInfo.ArgAt<Delivery>(0);
                    return new DeliveryBookingCommitResult(DeliveryBookingCommitStatus.Created);
                });
            var tracking = Substitute.For<IDeliveryTrackingIdGenerator>();
            tracking.Generate().Returns("PKL-ABCDEFGH");
            Handler = new(
                _quoteRepository,
                deliveryRepository,
                currencyRepository,
                Persistence,
                tracking,
                Options.Create(new DeliveryDispatchOptions { LeadTime = TimeSpan.FromMinutes(30) }),
                _timeProvider);
        }

        public Currency Currency { get; }
        public DeliveryQuote Quote { get; }
        public IDeliveryBookingPersistence Persistence { get; }
        public CreateDeliveryHandler Handler { get; }

        public CreateDeliveryCommand Command(
            string recipientName = "Ada Recipient",
            string recipientPhoneNumber = "+2348012345678",
            string? dropOffInstructions = "Blue gate") =>
            new(
                Quote.Id,
                recipientName,
                recipientPhoneNumber,
                dropOffInstructions,
                new ActorContext(_senderId, Guid.CreateVersion7(), "correlation", "flow"));

        public void Advance(TimeSpan duration) => _timeProvider.Advance(duration);

        public void HideQuote()
        {
            _quoteRepository.FirstOrDefaultAsync(
                    Arg.Any<ISpecification<DeliveryQuote>>(), Arg.Any<CancellationToken>())
                .Returns((DeliveryQuote?)null);
        }

        private static DeliveryAddress Address(string line, string? information) =>
            DeliveryAddress.Create(line, information, "Enugu", "Enugu", "NG", 6.45m, 7.51m);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
