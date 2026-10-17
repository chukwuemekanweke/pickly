using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreateDelivery;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.GetDelivery;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Bookings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.UnitTests.Features.Deliveries.Bookings;

public sealed class When_HandlingDeliveryBookingRequests_Should
{
    [Fact]
    public async Task ReturnCreatedThenAllowAuthenticatedRetrieval()
    {
        var context = new TestContext();
        var controller = context.Controller();

        var create = await controller.Create(context.Request(), "request-key", CancellationToken.None);
        var created = create.ShouldBeOfType<CreatedAtActionResult>();
        var response = created.Value.ShouldBeOfType<DeliveryResponse>();
        created.ActionName.ShouldBe(nameof(DeliveryBookingsController.Get));

        var get = await controller.Get(response.DeliveryId, CancellationToken.None);
        get.ShouldBeOfType<OkObjectResult>().Value.ShouldBeOfType<DeliveryResponse>()
            .DeliveryId.ShouldBe(response.DeliveryId);
    }

    [Fact]
    public async Task ReturnValidationProblemForInvalidRecipient()
    {
        var controller = new TestContext().Controller();

        var result = await controller.Create(
            new(Guid.Empty, new("", "08012345678"), new string('x', 501)),
            null,
            CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>().Value.ShouldBeOfType<ValidationProblemDetails>();
    }

    private sealed class TestContext
    {
        private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);
        private readonly Guid _senderId = Guid.CreateVersion7();
        private readonly IRepository<DeliveryQuote> _quotes = Substitute.For<IRepository<DeliveryQuote>>();
        private readonly IRepository<Delivery> _deliveries = Substitute.For<IRepository<Delivery>>();
        private readonly IRepository<DeliveryBookingConfirmation> _confirmations = Substitute.For<IRepository<DeliveryBookingConfirmation>>();
        private readonly IRepository<Currency> _currencies = Substitute.For<IRepository<Currency>>();
        private readonly IDeliveryBookingPersistence _persistence = Substitute.For<IDeliveryBookingPersistence>();
        private Delivery? _delivery;

        public TestContext()
        {
            var currency = Currency.Create("NGN", "Naira", true);
            var vehicle = VehicleCategory.Create("BIKE", "Bike", null, 50m, "driving");
            var zone = DeliveryZone.Create("ENUGU", "Enugu", "NG", 1, Now.AddDays(-1));
            var policy = DeliveryPricingPolicy.Create("v1", currency.Id, 100m, 0m, 0m, Now.AddDays(-1));
            Quote = DeliveryQuote.Create(
                _senderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                Address("Pickup", "Gate 1"),
                Address("Dropoff", null),
                DeliveryType.Instant,
                DeliveryOrderType.PayOnDelivery,
                null,
                vehicle,
                new(zone.Id, zone.Code, zone.Id, zone.Code),
                policy,
                new(100m, 0m, 0m, 0m, 0m),
                [("Parcel", 1, 1m)],
                Now,
                Now.AddMinutes(15));
            _quotes.FirstOrDefaultAsync(Arg.Any<ISpecification<DeliveryQuote>>(), Arg.Any<CancellationToken>())
                .Returns(Quote);
            _deliveries.FirstOrDefaultAsync(Arg.Any<ISpecification<Delivery>>(), Arg.Any<CancellationToken>())
                .Returns(_ => _delivery);
            _currencies.GetByIdAsync(currency.Id, Arg.Any<CancellationToken>()).Returns(currency);
            _persistence.CommitAsync(
                    Arg.Any<Delivery>(),
                    Arg.Any<DeliveryQuote>(),
                    Arg.Any<DeliveryBookingConfirmation>(),
                    Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    _delivery = callInfo.ArgAt<Delivery>(0);
                    return new DeliveryBookingCommitResult(DeliveryBookingCommitStatus.Created);
                });
        }

        public DeliveryQuote Quote { get; }

        public DeliveryBookingsController Controller()
        {
            var tracking = Substitute.For<IDeliveryTrackingIdGenerator>();
            tracking.Generate().Returns("PKL-ABCDEFGH");
            var create = new CreateDeliveryHandler(
                _quotes,
                _deliveries,
                _confirmations,
                _currencies,
                _persistence,
                tracking,
                Options.Create(new DeliveryDispatchOptions()),
                new FixedTimeProvider(Now));
            var get = new GetDeliveryHandler(_deliveries, _currencies);
            var actor = Substitute.For<ICurrentActor>();
            actor.ActorId.Returns(_senderId.ToString());
            actor.TenantId.Returns(Guid.CreateVersion7());
            actor.CorrelationId.Returns("correlation");
            actor.FlowId.Returns("flow");
            return new(create, get, new CreateDeliveryRequestValidator(), actor);
        }

        public CreateDeliveryRequest Request() =>
            new(Quote.Id, new("Ada Recipient", "+2348012345678"), "Blue gate");

        private static DeliveryAddress Address(string line, string? information) =>
            DeliveryAddress.Create(line, information, "Enugu", "Enugu", "NG", 6.45m, 7.51m);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
