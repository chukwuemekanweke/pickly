using BLVK.Pickly.Logistics.Application.Riders;
using BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.GetRiderStatus;
using BLVK.Pickly.Logistics.Application.Riders.Features.ListRiderOffers;
using BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;
using BLVK.Pickly.Logistics.Application.Riders.Features.RejectRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;
using BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace BLVK.Pickly.Logistics.Application.UnitTests.Riders;

public sealed class When_HandlingRiderOperations_Should
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly ActorContext Actor = new(Guid.CreateVersion7(), Guid.CreateVersion7(), "correlation", "flow");
    private static readonly RiderMatchingOptions MatchingOptions = new()
    {
        LocationFreshness = TimeSpan.FromMinutes(5),
        MaximumFutureLocationClockSkew = TimeSpan.FromSeconds(30),
        InitialSearchRadiusMeters = 3000,
        MaximumSearchRadiusMeters = 15000,
        SearchRadiusExpansionMeters = 3000,
        MaximumCandidatesPerRound = 20,
        OfferTimeout = TimeSpan.FromSeconds(30),
        MaximumAttemptsPerCycle = 5,
        RetryBackoff = TimeSpan.FromSeconds(30),
        MatchingScanBatchSize = 50,
        ScheduledDispatchScanBatchSize = 50,
        MaximumConcurrentAssignmentsPerRider = 1,
        MaximumTransactionRetryCount = 3,
        TransactionRetryBaseDelay = TimeSpan.FromMilliseconds(25)
    };

    [Fact]
    public async Task RejectUnauthenticatedLocationUpdateBeforeOpeningTransaction()
    {
        var transactions = new TestRiderTransactionFactory(new TestRiderTransaction());
        var handler = new UpdateRiderLocationHandler(
            EmptyActorResolver(),
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
        transactions.ExecutionCount.ShouldBe(0);
    }

    [Fact]
    public async Task RejectOutOfOrderLocationInHandler()
    {
        var profile = ApprovedProfile();
        var transaction = new TestRiderTransaction();
        var transactions = TransactionFactory(transaction);
        var persistence = Substitute.For<IRiderLocationPersistence>();
        persistence.FindAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(RiderCurrentLocation.Create(profile.Id, Now, Now));
        var telemetry = Substitute.For<IRiderMatchingTelemetry>();
        var handler = new UpdateRiderLocationHandler(
            ActorResolver(profile),
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
            EmptyActorResolver(),
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
    public async Task ReturnTemporaryUnavailableWhenTransactionRetriesAreExhausted()
    {
        var profile = ApprovedProfile();
        var handler = new UpdateRiderLocationHandler(
            ActorResolver(profile),
            new SelectivelyFailingTransactionFactory(profile.StakeholderId),
            Substitute.For<IRiderConsistencyLock>(),
            Substitute.For<IRiderLocationPersistence>(),
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(6.5m, 3.3m, Now, Actor), CancellationToken.None);

        result.Status.ShouldBe(UpdateRiderLocationStatus.TemporarilyUnavailable);
    }

    [Fact]
    public async Task RequireFreshLocationBeforeEnablingAvailability()
    {
        var profile = ApprovedProfile();
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
            ActorResolver(profile),
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
            ActorResolver(profile),
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
            ActorResolver(profile),
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
    public async Task ListActiveOffersWithoutMutatingOfferState()
    {
        var profile = ApprovedProfile();
        var reader = Substitute.For<IRiderOfferReader>();
        reader.ListActiveSnapshotsAsync(profile.Id, Now, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new ListRiderOffersHandler(ActorResolver(profile), reader, new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(Actor), CancellationToken.None);

        result.Status.ShouldBe(ListRiderOffersStatus.Success);
        await reader.Received(1).ListActiveSnapshotsAsync(profile.Id, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackOffSessionWhenAcceptingExpiredOffer()
    {
        var profile = AvailableProfile();
        var (delivery, session, offer, location) = AcceptanceState(profile);
        var persistence = AcceptancePersistence(profile, delivery, session, offer, location);
        var transaction = new TestRiderTransaction();
        var handler = new AcceptRiderOfferHandler(
            ActorResolver(profile),
            OfferRepository(offer),
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            NotificationStakeholders(),
            Substitute.For<ICommandSender>(),
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            NotificationOptions(),
            new FixedTimeProvider(offer.ExpiresAtUtc));

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Expired);
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Expired);
        session.NextAttemptAtUtc.ShouldBe(offer.ExpiresAtUtc + MatchingOptions.RetryBackoff);
        delivery.RiderId.ShouldBeNull();
        transaction.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task ForbidAcceptingAnotherRidersOfferBeforeOpeningTransaction()
    {
        var profile = ApprovedProfile();
        var persistence = Substitute.For<IRiderOfferAcceptancePersistence>();
        var foreignOffer = RiderDeliveryOffer.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Now,
            Now.AddMinutes(1));
        var transactions = new TestRiderTransactionFactory(new TestRiderTransaction());
        var handler = new AcceptRiderOfferHandler(
            ActorResolver(profile),
            OfferRepository(foreignOffer),
            transactions,
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            NotificationStakeholders(),
            Substitute.For<ICommandSender>(),
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            NotificationOptions(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(foreignOffer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Forbidden);
        transactions.ExecutionCount.ShouldBe(0);
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
        var commandSender = Substitute.For<ICommandSender>();
        var handler = AcceptanceHandler(profile, offer, persistence, transaction, telemetry, commandSender);

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Conflict);
        delivery.RiderId.ShouldBeNull();
        transaction.Committed.ShouldBeFalse();
        telemetry.Received(1).RecordAssignmentConflict();
        await commandSender.DidNotReceive().SendAsync(
            Arg.Any<SendNotificationCommand>(),
            Arg.Any<CancellationToken>());
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
        var commandSender = Substitute.For<ICommandSender>();
        var handler = AcceptanceHandler(profile, offer, persistence, transaction, telemetry, commandSender);

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Success);
        delivery.RiderId.ShouldBe(profile.StakeholderId);
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Accepted);
        session.Status.ShouldBe(DeliveryMatchingSessionStatus.Assigned);
        transaction.Committed.ShouldBeTrue();
        telemetry.Received(1).RecordOfferAccepted();
        await commandSender.Received(2).SendAsync(
            Arg.Is<SendNotificationCommand>(notification =>
                notification.NotificationType == NotificationType.RiderAssigned ||
                notification.NotificationType == NotificationType.RiderAcceptedInstructions),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmAcceptedAssignmentAfterAmbiguousCommit()
    {
        var profile = AvailableProfile();
        var (delivery, _, offer, _) = AcceptanceState(profile);
        delivery.AssignRider(profile.StakeholderId, Now, "rider", "ambiguous_commit_test");
        var persistence = Substitute.For<IRiderOfferAcceptancePersistence>();
        persistence.FindAcceptedDeliveryAsync(
                offer.Id,
                profile.Id,
                profile.StakeholderId,
                Arg.Any<CancellationToken>())
            .Returns(delivery);
        var handler = new AcceptRiderOfferHandler(
            ActorResolver(profile),
            OfferRepository(offer),
            new AmbiguousCommitTransactionFactory(),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            NotificationStakeholders(),
            Substitute.For<ICommandSender>(),
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            NotificationOptions(),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(AcceptRiderOfferStatus.Success);
        result.Delivery.ShouldNotBeNull();
        result.Delivery.DeliveryId.ShouldBe(delivery.Id);
    }

    [Fact]
    public async Task CompleteEmptyMatchingIterationThroughApplicationHandler()
    {
        var persistence = Substitute.For<IRiderMatchingPersistence>();
        persistence.ListExpiredOfferCandidatesAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.ListScheduledDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.ListMatchingEligibleDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var transaction = new TestRiderTransaction();
        var handler = new ProcessRiderMatchingHandler(
            TransactionFactory(transaction),
            GrantingLock(),
            persistence,
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now),
            NullLogger<ProcessRiderMatchingHandler>.Instance);

        var result = await handler.HandleAsync(new(), CancellationToken.None);

        result.ShouldBe(new ProcessRiderMatchingResult(0, 0, 0, 0, 0));
        transaction.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task IsolateFailedDeliveryWhileContinuingMatchingIteration()
    {
        var firstDelivery = AcceptanceState(AvailableProfile()).Delivery;
        var secondDelivery = AcceptanceState(AvailableProfile()).Delivery;
        var persistence = Substitute.For<IRiderMatchingPersistence>();
        persistence.ListExpiredOfferCandidatesAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.ListScheduledDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.ListMatchingEligibleDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([firstDelivery.Id, secondDelivery.Id]);
        persistence.LoadMatchingStateAsync(secondDelivery.Id, Arg.Any<CancellationToken>())
            .Returns(new RiderMatchingState(secondDelivery, null, false));
        persistence.FindCandidateAsync(
                secondDelivery,
                Arg.Any<int>(),
                Arg.Any<DateTimeOffset>(),
                Now,
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((RiderCandidate?)null);
        var telemetry = Substitute.For<IRiderMatchingTelemetry>();
        var handler = new ProcessRiderMatchingHandler(
            new SelectivelyFailingTransactionFactory(firstDelivery.Id),
            GrantingLock(),
            persistence,
            telemetry,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now),
            NullLogger<ProcessRiderMatchingHandler>.Instance);

        var result = await handler.HandleAsync(new(), CancellationToken.None);

        result.FailedOperations.ShouldBe(1);
        result.MatchingAttempts.ShouldBe(1);
        result.NoCandidateAttempts.ShouldBe(1);
        telemetry.Received(1).RecordProcessingFailures(1);
    }

    [Fact]
    public async Task SkipDeliveryLockedByAnotherWorker()
    {
        var delivery = AcceptanceState(AvailableProfile()).Delivery;
        var persistence = EmptyMatchingPersistence();
        persistence.ListMatchingEligibleDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([delivery.Id]);
        var consistencyLock = Substitute.For<IRiderConsistencyLock>();
        consistencyLock.TryAcquireDeliveryAsync(delivery.Id, Arg.Any<CancellationToken>()).Returns(false);
        var handler = new ProcessRiderMatchingHandler(
            TransactionFactory(new TestRiderTransaction()),
            consistencyLock,
            persistence,
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now),
            NullLogger<ProcessRiderMatchingHandler>.Instance);

        var result = await handler.HandleAsync(new(), CancellationToken.None);

        result.ShouldBe(new ProcessRiderMatchingResult(0, 0, 0, 0, 0));
        await persistence.DidNotReceiveWithAnyArgs().LoadMatchingStateAsync(default, default);
    }

    [Fact]
    public async Task IsolateUnexpectedFailureWhileContinuingMatchingIteration()
    {
        var firstDelivery = AcceptanceState(AvailableProfile()).Delivery;
        var secondDelivery = AcceptanceState(AvailableProfile()).Delivery;
        var persistence = EmptyMatchingPersistence();
        persistence.ListMatchingEligibleDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([firstDelivery.Id, secondDelivery.Id]);
        persistence.LoadMatchingStateAsync(firstDelivery.Id, Arg.Any<CancellationToken>())
            .Returns<RiderMatchingState>(_ => throw new InvalidOperationException("Unexpected failure."));
        persistence.LoadMatchingStateAsync(secondDelivery.Id, Arg.Any<CancellationToken>())
            .Returns(new RiderMatchingState(secondDelivery, null, false));
        var handler = new ProcessRiderMatchingHandler(
            TransactionFactory(new TestRiderTransaction()),
            GrantingLock(),
            persistence,
            Substitute.For<IRiderMatchingTelemetry>(),
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now),
            NullLogger<ProcessRiderMatchingHandler>.Instance);

        var result = await handler.HandleAsync(new(), CancellationToken.None);

        result.FailedOperations.ShouldBe(1);
        result.MatchingAttempts.ShouldBe(1);
        result.NoCandidateAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task RelockWithOfferDeliveryBeforeRevokingOfferCreatedAfterPreview()
    {
        var profile = AvailableProfile();
        var (delivery, session, offer, location) = AcceptanceState(profile);
        var persistence = Substitute.For<IRiderAvailabilityPersistence>();
        persistence.ListPendingOfferDeliveryIdsAsync(profile.Id, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Guid>());
        persistence.LoadAsync(profile, Arg.Any<CancellationToken>())
            .Returns(new RiderAvailabilityState(
                location,
                false,
                true,
                [offer],
                new Dictionary<Guid, DeliveryMatchingSession> { [session.Id] = session }));
        var statusReader = Substitute.For<IRiderStatusReader>();
        statusReader.GetFactsAsync(profile, Now, Arg.Any<CancellationToken>())
            .Returns(new RiderStatusFacts(location, false, 0));
        var consistencyLock = Substitute.For<IRiderConsistencyLock>();
        var transaction = new TestRiderTransaction();
        var transactions = new TestRiderTransactionFactory(transaction);
        var handler = new SetRiderAvailabilityHandler(
            ActorResolver(profile),
            new RiderStatusProjector(statusReader),
            transactions,
            consistencyLock,
            persistence,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(false, Actor), CancellationToken.None);

        result.Status.ShouldBe(SetRiderAvailabilityStatus.Success);
        transactions.ExecutionCount.ShouldBe(2);
        await consistencyLock.Received(1).AcquireDeliveriesThenRiderAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 0),
            profile.StakeholderId,
            Arg.Any<CancellationToken>());
        await consistencyLock.Received(1).AcquireDeliveriesThenRiderAsync(
            Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(delivery.Id)),
            profile.StakeholderId,
            Arg.Any<CancellationToken>());
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Revoked);
        session.NextAttemptAtUtc.ShouldBe(Now + MatchingOptions.RetryBackoff);
        profile.HasEnabledAvailability.ShouldBeFalse();
        transaction.Committed.ShouldBeTrue();
    }

    [Fact]
    public async Task RejectOfferAndBackOffSessionThroughRepositories()
    {
        var profile = AvailableProfile();
        var (_, session, offer, _) = AcceptanceState(profile);
        var sessions = Substitute.For<IRepository<DeliveryMatchingSession>>();
        sessions.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var transaction = new TestRiderTransaction();
        var telemetry = Substitute.For<IRiderMatchingTelemetry>();
        var handler = new RejectRiderOfferHandler(
            ActorResolver(profile),
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            OfferRepository(offer),
            sessions,
            telemetry,
            Options.Create(MatchingOptions),
            new FixedTimeProvider(Now));

        var result = await handler.HandleAsync(new(offer.Id, Actor), CancellationToken.None);

        result.Status.ShouldBe(RejectRiderOfferStatus.Success);
        offer.Status.ShouldBe(RiderDeliveryOfferStatus.Rejected);
        session.NextAttemptAtUtc.ShouldBe(Now + MatchingOptions.RetryBackoff);
        transaction.Committed.ShouldBeTrue();
        telemetry.Received(1).RecordOfferRejected();
    }

    private static IRiderMatchingPersistence EmptyMatchingPersistence()
    {
        var persistence = Substitute.For<IRiderMatchingPersistence>();
        persistence.ListExpiredOfferCandidatesAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        persistence.ListScheduledDeliveryIdsAsync(Now, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
        return persistence;
    }

    private static IRiderConsistencyLock GrantingLock()
    {
        var consistencyLock = Substitute.For<IRiderConsistencyLock>();
        consistencyLock.TryAcquireDeliveryAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        return consistencyLock;
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
        RiderDeliveryOffer offer,
        IRiderOfferAcceptancePersistence persistence,
        IRiderTransaction transaction,
        IRiderMatchingTelemetry telemetry,
        ICommandSender commandSender) =>
        new(
            ActorResolver(profile),
            OfferRepository(offer),
            TransactionFactory(transaction),
            Substitute.For<IRiderConsistencyLock>(),
            persistence,
            NotificationStakeholders(),
            commandSender,
            telemetry,
            Options.Create(MatchingOptions),
            NotificationOptions(),
            new FixedTimeProvider(Now));

    private static IStakeholderReadModelRepository NotificationStakeholders()
    {
        var stakeholders = Substitute.For<IStakeholderReadModelRepository>();
        stakeholders.GetByStakeholderIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var stakeholderId = callInfo.ArgAt<Guid>(0);
                return new StakeholderReadModel(
                    stakeholderId,
                    Guid.CreateVersion7(),
                    $"{stakeholderId:N}@example.com",
                    Actor.TenantId!.Value,
                    Guid.CreateVersion7(),
                    Guid.CreateVersion7(),
                    "Test",
                    "Person",
                    null,
                    true);
            });
        return stakeholders;
    }

    private static IOptions<DeliveryNotificationOptions> NotificationOptions() =>
        Options.Create(new DeliveryNotificationOptions
        {
            ApplicationBaseUrl = "https://app.pikly.org/",
            DisplayTimeZoneId = "Africa/Lagos"
        });

    private static RiderActorResolver ActorResolver(RiderProfile profile)
    {
        var stakeholderType = StakeholderType.Create(Actor.TenantId!.Value, "Rider", "rider");
        var stakeholder = Stakeholder.Create(
            Guid.CreateVersion7(),
            Actor.TenantId.Value,
            Guid.CreateVersion7(),
            stakeholderType.Id,
            "Test",
            "Rider");
        stakeholder.MarkVerified();
        typeof(Stakeholder).GetProperty(nameof(Stakeholder.StakeholderType))!
            .SetValue(stakeholder, stakeholderType);
        var stakeholders = Substitute.For<IRepository<Stakeholder>>();
        stakeholders.FirstOrDefaultAsync(
                Arg.Any<ISpecification<Stakeholder>>(),
                Arg.Any<CancellationToken>())
            .Returns(stakeholder);
        var profiles = Substitute.For<IRepository<RiderProfile>>();
        profiles.FirstOrDefaultAsync(
                Arg.Any<ISpecification<RiderProfile>>(),
                Arg.Any<CancellationToken>())
            .Returns(profile);
        return new(stakeholders, profiles);
    }

    private static IRepository<RiderDeliveryOffer> OfferRepository(RiderDeliveryOffer offer)
    {
        var offers = Substitute.For<IRepository<RiderDeliveryOffer>>();
        offers.FirstOrDefaultAsync(Arg.Any<ISpecification<RiderDeliveryOffer>>(), Arg.Any<CancellationToken>())
            .Returns(offer);
        offers.GetByIdAsync(offer.Id, Arg.Any<CancellationToken>()).Returns(offer);
        return offers;
    }

    private static RiderActorResolver EmptyActorResolver() =>
        new(
            Substitute.For<IRepository<Stakeholder>>(),
            Substitute.For<IRepository<RiderProfile>>());

    private static IRiderTransactionFactory TransactionFactory(IRiderTransaction transaction) =>
        new TestRiderTransactionFactory(transaction);

    private sealed class TestRiderTransactionFactory(IRiderTransaction transaction) : IRiderTransactionFactory
    {
        public int ExecutionCount { get; private set; }

        public Task<TResult> ExecuteAsync<TResult>(
            RiderTransactionIsolation isolation,
            RiderTransactionContext context,
            Func<IRiderTransaction, CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return operation(transaction, cancellationToken);
        }
    }

    private sealed class SelectivelyFailingTransactionFactory(Guid failedResourceId) : IRiderTransactionFactory
    {
        public Task<TResult> ExecuteAsync<TResult>(
            RiderTransactionIsolation isolation,
            RiderTransactionContext context,
            Func<IRiderTransaction, CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            if (context.ResourceId == failedResourceId)
            {
                throw new RiderTransientPersistenceException("Forced failure.", 4, new Exception("Forced failure."));
            }

            return operation(new TestRiderTransaction(), cancellationToken);
        }
    }

    private sealed class AmbiguousCommitTransactionFactory : IRiderTransactionFactory
    {
        public Task<TResult> ExecuteAsync<TResult>(
            RiderTransactionIsolation isolation,
            RiderTransactionContext context,
            Func<IRiderTransaction, CancellationToken, Task<TResult>> operation,
            CancellationToken cancellationToken) =>
            throw new RiderCommitOutcomeUnknownException(
                "Forced ambiguous commit.",
                new Exception("Forced ambiguous commit."));
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
