using BLVK.Pickly.Logistics.Application.Riders;
using BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderStatus;
using BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;
using BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;
using BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;
using BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Riders;

public sealed class When_HandlingRiderOperations_Should
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly ActorContext Actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");
    private static readonly RiderMatchingOptions MatchingOptions = new() { OfferTimeout = TimeSpan.FromSeconds(30) };

    [Fact]
    public async Task RejectUnauthenticatedLocationUpdateBeforeOpeningTransaction()
    {
        var actorReader = Substitute.For<IRiderActorReader>();
        var transactions = Substitute.For<IRiderTransactionFactory>();
        var handler = new UpdateRiderLocationHandler(
            new RiderActorResolver(actorReader),
            transactions,
            Substitute.For<IRiderConsistencyLock>(),
            Substitute.For<IRiderLocationPersistence>(),
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new(6.5m, 3.3m, Now, new(null, null, "correlation", "flow")),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateRiderLocationStatus.NotAuthenticated);
        await transactions.DidNotReceiveWithAnyArgs().BeginAsync(default, default);
    }

    [Fact]
    public async Task RejectOutOfOrderLocationInHandler()
    {
        var profile = ApprovedProfile();
        var actorReader = ActorReader(profile);
        var transaction = new TestRiderTransaction();
        var transactions = TransactionFactory(transaction);
        var persistence = Substitute.For<IRiderLocationPersistence>();
        persistence.FindAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(RiderCurrentLocation.Create(profile.Id, Now, Now));
        var telemetry = Substitute.For<IRiderMatchingTelemetry>();
        var handler = new UpdateRiderLocationHandler(
            new RiderActorResolver(actorReader),
            transactions,
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            telemetry,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(
            new(6.5m, 3.3m, Now.AddSeconds(-1), Actor),
            CancellationToken.None);

        result.Status.ShouldBe(UpdateRiderLocationStatus.StaleLocation);
        transaction.Committed.ShouldBeFalse();
        telemetry.Received(1).RecordStaleLocation();
    }

    [Fact]
    public async Task RejectInvalidCoordinatesBeforeReadingPersistence()
    {
        var persistence = Substitute.For<IRiderLocationPersistence>();
        var handler = new UpdateRiderLocationHandler(
            new RiderActorResolver(Substitute.For<IRiderActorReader>()),
            Substitute.For<IRiderTransactionFactory>(),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(91m, 3.3m, Now, Actor), CancellationToken.None);

        result.Status.ShouldBe(UpdateRiderLocationStatus.InvalidLocation);
        await persistence.DidNotReceiveWithAnyArgs().FindAsync(default, default);
    }

    [Fact]
    public async Task RequireFreshLocationBeforeEnablingAvailability()
    {
        var profile = ApprovedProfile();
        var actorReader = ActorReader(profile);
        var persistence = Substitute.For<IRiderAvailabilityPersistence>();
        persistence.LoadAsync(profile, Arg.Any<CancellationToken>())
            .Returns(new RiderAvailabilityState(
                null,
                false,
                true,
                [],
                new Dictionary<Guid, DeliveryMatchingSession>()));
        var statusReader = Substitute.For<IRiderStatusReader>();
        var transaction = new TestRiderTransaction();
        var handler = new SetRiderAvailabilityHandler(
            new RiderActorResolver(actorReader),
            new RiderStatusProjector(statusReader),
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(true, Actor), CancellationToken.None);

        result.Status.ShouldBe(SetRiderAvailabilityStatus.StaleLocation);
        profile.HasEnabledAvailability.ShouldBeFalse();
        transaction.Committed.ShouldBeFalse();
    }

    [Fact]
    public async Task PreventAvailabilityWhileRiderHasActiveDelivery()
    {
        var profile = ApprovedProfile();
        var location = RiderCurrentLocation.Create(profile.Id, Now, Now);
        var persistence = Substitute.For<IRiderAvailabilityPersistence>();
        persistence.LoadAsync(profile, Arg.Any<CancellationToken>())
            .Returns(new RiderAvailabilityState(
                location,
                true,
                true,
                [],
                new Dictionary<Guid, DeliveryMatchingSession>()));
        var transaction = new TestRiderTransaction();
        var handler = new SetRiderAvailabilityHandler(
            new RiderActorResolver(ActorReader(profile)),
            new RiderStatusProjector(Substitute.For<IRiderStatusReader>()),
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(true, Actor), CancellationToken.None);

        result.Status.ShouldBe(SetRiderAvailabilityStatus.Conflict);
        profile.HasEnabledAvailability.ShouldBeFalse();
        transaction.Committed.ShouldBeFalse();
    }

    [Fact]
    public async Task ProjectOperationalStatusInApplication()
    {
        var profile = ApprovedProfile();
        profile.SetAvailability(true);
        var location = RiderCurrentLocation.Create(profile.Id, Now, Now);
        var reader = Substitute.For<IRiderStatusReader>();
        reader.GetFactsAsync(profile, Now, Arg.Any<CancellationToken>())
            .Returns(new RiderStatusFacts(location, false, 2));
        var handler = new GetRiderStatusHandler(
            new RiderActorResolver(ActorReader(profile)),
            new RiderStatusProjector(reader),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(Actor), CancellationToken.None);

        result.Status.ShouldBe(GetRiderStatusStatus.Success);
        result.RiderStatus.ShouldNotBeNull();
        result.RiderStatus.OperationalStatus.ShouldBe("Available");
        result.RiderStatus.PendingOfferCount.ShouldBe(2);
    }

    [Fact]
    public async Task ExpireOffersInApplicationBeforeReturningActiveOffers()
    {
        var profile = ApprovedProfile();
        var session = DeliveryMatchingSession.Create(Guid.CreateVersion7(), 3000, Now.AddMinutes(-2));
        var expired = RiderDeliveryOffer.Create(
            session.Id,
            session.DeliveryId,
            profile.Id,
            Now.AddMinutes(-2),
            Now.AddMinutes(-1));
        var reader = Substitute.For<IRiderOfferReader>();
        reader.ListExpiredAsync(profile.Id, Now, Arg.Any<CancellationToken>()).Returns([expired]);
        reader.ListActiveSnapshotsAsync(profile.Id, Now, Arg.Any<CancellationToken>()).Returns([]);
        var transaction = new TestRiderTransaction();
        var handler = new ListRiderOffersHandler(
            new RiderActorResolver(ActorReader(profile)),
            reader,
            TransactionFactory(transaction),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(Actor), CancellationToken.None);

        result.Status.ShouldBe(ListRiderOffersStatus.Success);
        expired.Status.ShouldBe(RiderDeliveryOfferStatus.Expired);
        transaction.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task ForbidAcceptingAnotherRidersOfferBeforeOpeningTransaction()
    {
        var profile = ApprovedProfile();
        var offerId = Guid.CreateVersion7();
        var persistence = Substitute.For<IRiderOfferAcceptancePersistence>();
        persistence.FindPreviewAsync(offerId, Arg.Any<CancellationToken>())
            .Returns(new RiderOfferPreview(Guid.CreateVersion7(), Guid.CreateVersion7()));
        var transactions = Substitute.For<IRiderTransactionFactory>();
        var handler = new AcceptRiderOfferHandler(
            new RiderActorResolver(ActorReader(profile)),
            transactions,
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(offerId, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Forbidden);
        await transactions.DidNotReceiveWithAnyArgs().BeginAsync(default, default);
    }

    [Fact]
    public async Task RejectAcceptanceWhenGeographicFactsAreIneligible()
    {
        var profile = AvailableProfile();
        var (delivery, session, offer, location) = AcceptanceState(profile);
        var persistence = AcceptancePersistence(profile, delivery, session, offer, location);
        persistence.GetGeographicEligibilityAsync(
                profile,
                delivery,
                location,
                session.SearchRadiusMeters,
                Now,
                Arg.Any<CancellationToken>())
            .Returns(new RiderGeographicEligibility(true, false, true));
        var transaction = new TestRiderTransaction();
        var telemetry = Substitute.For<IRiderMatchingTelemetry>();
        var handler = AcceptanceHandler(profile, persistence, transaction, telemetry);

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Conflict);
        delivery.RiderId.ShouldBeNull();
        transaction.Committed.ShouldBeFalse();
        telemetry.Received(1).RecordAssignmentConflict();
    }

    [Fact]
    public async Task AssignDeliveryAndCompleteSessionInApplication()
    {
        var profile = AvailableProfile();
        var (delivery, session, offer, location) = AcceptanceState(profile);
        var persistence = AcceptancePersistence(profile, delivery, session, offer, location);
        persistence.GetGeographicEligibilityAsync(
                profile,
                delivery,
                location,
                session.SearchRadiusMeters,
                Now,
                Arg.Any<CancellationToken>())
            .Returns(new RiderGeographicEligibility(true, true, true));
        var transaction = new TestRiderTransaction();
        var telemetry = Substitute.For<IRiderMatchingTelemetry>();
        var handler = AcceptanceHandler(profile, persistence, transaction, telemetry);

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Success);
        delivery.RiderId.ShouldBe(profile.StakeholderId);
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Accepted);
        session.Status.ShouldBe(DeliveryMatchingSessionStatus.Assigned);
        transaction.Committed.ShouldBeTrue();
        telemetry.Received(1).RecordOfferAccepted();
    }

    [Fact]
    public async Task CompleteEmptyMatchingIterationThroughApplicationHandler()
    {
        var persistence = Substitute.For<IRiderMatchingPersistence>();
        persistence.ListExpiredOffersAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.GetMatchingSessionsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DeliveryMatchingSession>());
        persistence.ListScheduledDeliveriesAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.ListMatchingEligibleDeliveriesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.GetActiveSessionsByDeliveryAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, DeliveryMatchingSession>());
        persistence.GetDeliveriesWithPendingOffersAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<Guid>());
        var transaction = new TestRiderTransaction();
        var handler = new ProcessRiderMatchingHandler(
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(), CancellationToken.None);

        result.ShouldBe(new ProcessRiderMatchingResult(0, 0, 0, 0, 0));
        transaction.Flushed.ShouldBeTrue();
        transaction.Committed.ShouldBeTrue();
    }

    private static RiderProfile ApprovedProfile()
    {
        var profile = RiderProfile.Create(Actor.StakeholderId!.Value, Actor.TenantId!.Value, Guid.CreateVersion7());
        profile.Approve();
        return profile;
    }

    private static RiderProfile AvailableProfile()
    {
        var profile = ApprovedProfile();
        profile.SetAvailability(true);
        return profile;
    }

    private static (Delivery Delivery, DeliveryMatchingSession Session, RiderDeliveryOffer Offer, RiderCurrentLocation Location)
        AcceptanceState(RiderProfile profile)
    {
        var delivery = Delivery.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            profile.VehicleCategoryId,
            "PKY-RIDER-TEST",
            DeliveryType.Instant,
            DeliveryOrderType.PayOnDelivery,
            DeliveryAddress.Create("1 Pickup", null, "Lagos", "Lagos", "NG", 6.5m, 3.3m),
            DeliveryAddress.Create("2 Dropoff", null, "Lagos", "Lagos", "NG", 6.6m, 3.4m),
            DeliveryRecipient.Create("Recipient", "+2348012345678"),
            DeliveryCharge.Create(2500m, Guid.CreateVersion7()),
            [DeliveryItem.Create("Parcel", 1, 1m)],
            Now.AddMinutes(-5));
        var session = DeliveryMatchingSession.Create(delivery.Id, 3000, Now.AddMinutes(-1));
        var offer = RiderDeliveryOffer.Create(session.Id, delivery.Id, profile.Id, Now.AddSeconds(-10), Now.AddMinutes(1));
        var location = RiderCurrentLocation.Create(profile.Id, Now, Now);
        return (delivery, session, offer, location);
    }

    private static IRiderOfferAcceptancePersistence AcceptancePersistence(
        RiderProfile profile,
        Delivery delivery,
        DeliveryMatchingSession session,
        RiderDeliveryOffer offer,
        RiderCurrentLocation location)
    {
        var persistence = Substitute.For<IRiderOfferAcceptancePersistence>();
        persistence.FindPreviewAsync(offer.Id, Arg.Any<CancellationToken>())
            .Returns(new RiderOfferPreview(delivery.Id, profile.Id));
        persistence.LoadAsync(offer.Id, profile.Id, Arg.Any<CancellationToken>())
            .Returns(new RiderOfferAcceptanceState(offer, delivery, session, location));
        persistence.IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, Arg.Any<CancellationToken>())
            .Returns(true);
        persistence.HasActiveDeliveryAsync(profile.StakeholderId, Arg.Any<CancellationToken>())
            .Returns(false);
        persistence.ListConflictingOffersAsync(
                offer.Id,
                delivery.Id,
                profile.Id,
                Arg.Any<CancellationToken>())
            .Returns([]);
        return persistence;
    }

    private static AcceptRiderOfferHandler AcceptanceHandler(
        RiderProfile profile,
        IRiderOfferAcceptancePersistence persistence,
        IRiderTransaction transaction,
        IRiderMatchingTelemetry telemetry) =>
        new(
            new RiderActorResolver(ActorReader(profile)),
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            telemetry,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

    private static IRiderActorReader ActorReader(RiderProfile profile)
    {
        var reader = Substitute.For<IRiderActorReader>();
        reader.FindAsync(
                Actor.StakeholderId!.Value,
                Actor.TenantId,
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(profile);
        return reader;
    }

    private static IRiderTransactionFactory TransactionFactory(IRiderTransaction transaction)
    {
        var factory = Substitute.For<IRiderTransactionFactory>();
        factory.BeginAsync(Arg.Any<RiderTransactionIsolation>(), Arg.Any<CancellationToken>())
            .Returns(transaction);
        return factory;
    }

    private sealed class TestRiderTransaction : IRiderTransaction
    {
        public bool Flushed { get; private set; }
        public bool Committed { get; private set; }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            Flushed = true;
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
