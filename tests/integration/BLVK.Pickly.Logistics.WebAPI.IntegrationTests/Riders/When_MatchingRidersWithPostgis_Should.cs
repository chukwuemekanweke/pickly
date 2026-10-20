using BLVK.Pickly.Logistics.Application.Riders;
using BLVK.Pickly.Logistics.Application.Riders.Features.AcceptRiderOffer;
using BLVK.Pickly.Logistics.Application.Riders.Features.ProcessRiderMatching;
using BLVK.Pickly.Logistics.Application.Riders.Features.SetRiderAvailability;
using BLVK.Pickly.Logistics.Application.Riders.Features.UpdateRiderLocation;
using BLVK.Pickly.Logistics.Contracts.Commands.Notifications;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Messaging;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.ReadModels;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Shouldly;
using System.Diagnostics;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Riders;

[Collection(nameof(ContainersCollection))]
public sealed class When_MatchingRidersWithPostgis_Should(ContainersFixture fixture)
    : DeliveryPersistenceIntegrationTestBase(fixture)
{
    [Fact]
    public async Task DiscoverNearestEligibleRiderAndAssignAtomically()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var options = MatchingOptions();
        var now = Now.AddHours(2);
        var matching = CreateMatchingHandler(scope.ServiceProvider, options, now);
        var acceptance = CreateAcceptanceHandler(scope.ServiceProvider, options, now.AddSeconds(1));

        var profile = await dbContext.RiderProfiles.SingleAsync(item => item.StakeholderId == rider.StakeholderId);
        profile.VerificationStatus.ShouldBe(RiderVerificationStatus.Approved);
        profile.AvailabilityStatus.ShouldBe(RiderAvailabilityStatus.Available);
        var pickupPoint = new Point((double)delivery.PickupAddress.Longitude, (double)delivery.PickupAddress.Latitude) { SRID = 4326 };
        var distance = await dbContext.RiderCurrentLocations
            .Where(item => item.RiderProfileId == profile.Id)
            .Select(item => EF.Property<Point>(item, RiderCurrentLocationConfiguration.PositionProperty).Distance(pickupPoint))
            .SingleAsync();
        distance.ShouldBeLessThan(options.InitialSearchRadiusMeters);
        var quote = await dbContext.DeliveryQuotes.AsNoTracking().SingleAsync(item => item.Id == delivery.DeliveryQuoteId);
        (await dbContext.RiderServiceZones.AnyAsync(item =>
            item.RiderProfileId == profile.Id && item.DeliveryZoneId == quote.OriginZoneId)).ShouldBeTrue();
        (await dbContext.DeliveryZones.AnyAsync(item =>
            item.Id == quote.OriginZoneId &&
            EF.Property<MultiPolygon>(item, DeliveryZoneConfiguration.BoundaryProperty).Covers(pickupPoint))).ShouldBeTrue();
        (await dbContext.Stakeholders
            .Where(item => item.Id == rider.StakeholderId)
            .Select(item => item.StakeholderType.Key)
            .SingleAsync()).ShouldBe("rider");

        var processing = await matching.HandleAsync(new(), CancellationToken.None);
        processing.MatchingAttempts.ShouldBeGreaterThan(0);

        var offer = await dbContext.RiderDeliveryOffers.SingleOrDefaultAsync(item => item.DeliveryId == delivery.Id);
        offer.ShouldNotBeNull($"Attempts={processing.MatchingAttempts}, NoCandidates={processing.NoCandidateAttempts}");
        dbContext.ChangeTracker.Clear();
        var accepted = await acceptance.HandleAsync(
            new(offer.Id, new(rider.StakeholderId, rider.TenantId, "test-correlation", "test-flow")),
            CancellationToken.None);

        accepted.Status.ShouldBe(AcceptRiderOfferStatus.Success);
        accepted.Delivery.ShouldNotBeNull();
        accepted.Delivery.Status.ShouldBe("RiderAssigned");

        var persisted = await dbContext.Deliveries
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == delivery.Id);
        persisted.RiderId.ShouldBe(rider.StakeholderId);
        persisted.StatusHistory.ShouldContain(item => item.NewStatus.ToString() == "RiderAssigned");
    }

    [Fact]
    public async Task ExcludeRiderOutsideConfiguredRadius()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        await PersistRiderAsync(delivery, 6.58, 3.44);

        using var scope = CreateScope();
        var options = MatchingOptions();
        var matching = CreateMatchingHandler(scope.ServiceProvider, options, Now.AddHours(2));
        var processing = await matching.HandleAsync(new(), CancellationToken.None);

        processing.NoCandidateAttempts.ShouldBeGreaterThan(0);
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
            .AnyAsync(item => item.DeliveryId == delivery.Id)).ShouldBeFalse();
    }

    private static ProcessRiderMatchingHandler CreateMatchingHandler(
        IServiceProvider services,
        RiderMatchingOptions options,
        DateTimeOffset utcNow) =>
        new(
            services.GetRequiredService<IRiderTransactionFactory>(),
            services.GetRequiredService<IRiderConsistencyLock>(),
            services.GetRequiredService<IRiderMatchingPersistence>(),
            services.GetRequiredService<IRiderMatchingTelemetry>(),
            Options.Create(options),
            new FixedTimeProvider(utcNow),
            services.GetRequiredService<ILogger<ProcessRiderMatchingHandler>>());

    private static AcceptRiderOfferHandler CreateAcceptanceHandler(
        IServiceProvider services,
        RiderMatchingOptions options,
        DateTimeOffset utcNow) =>
        new(
            services.GetRequiredService<RiderActorResolver>(),
            services.GetRequiredService<IRepository<RiderDeliveryOffer>>(),
            services.GetRequiredService<IRiderTransactionFactory>(),
            services.GetRequiredService<IRiderConsistencyLock>(),
            services.GetRequiredService<IRiderOfferAcceptancePersistence>(),
            services.GetRequiredService<IStakeholderReadModelRepository>(),
            services.GetRequiredService<ICommandSender>(),
            services.GetRequiredService<IRiderMatchingTelemetry>(),
            Options.Create(options),
            Options.Create(new DeliveryNotificationOptions
            {
                ApplicationBaseUrl = "https://app.pikly.ng/",
                DisplayTimeZoneId = "Africa/Lagos"
            }),
            new FixedTimeProvider(utcNow));

    private static SetRiderAvailabilityHandler CreateAvailabilityHandler(
        IServiceProvider services,
        RiderMatchingOptions options,
        DateTimeOffset utcNow) =>
        new(
            services.GetRequiredService<RiderActorResolver>(),
            services.GetRequiredService<RiderStatusProjector>(),
            services.GetRequiredService<IRiderTransactionFactory>(),
            services.GetRequiredService<IRiderConsistencyLock>(),
            services.GetRequiredService<IRiderAvailabilityPersistence>(),
            Options.Create(options),
            new FixedTimeProvider(utcNow));

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public async Task EnforceSingleActiveDeliveryPerRiderDuringConcurrentAssignments()
    {
        var (first, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var (second, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(first, 6.5250, 3.3800);

        async Task<bool> TryAssignAsync(Guid deliveryId)
        {
            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var delivery = await dbContext.Deliveries.SingleAsync(item => item.Id == deliveryId);
            delivery.AssignRider(rider.StakeholderId, Now.AddHours(2), "test-rider", "concurrency_test");
            dbContext.DeliveryStatusHistory.Add(delivery.StatusHistory.Last());
            try
            {
                await dbContext.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        var results = await Task.WhenAll(TryAssignAsync(first.Id), TryAssignAsync(second.Id));
        results.Count(result => result).ShouldBe(1);
        results.Count(result => !result).ShouldBe(1);

        using var assertionScope = CreateScope();
        (await assertionScope.ServiceProvider.GetRequiredService<AppDbContext>().Deliveries.CountAsync(item =>
            item.RiderId == rider.StakeholderId &&
            (item.Status == DeliveryStatus.RiderAssigned ||
                item.Status == DeliveryStatus.PickedUp ||
                item.Status == DeliveryStatus.InTransit))).ShouldBe(1);
    }

    [Fact]
    public async Task AllowConcurrentProcessorsWithoutDuplicateSessionOrOffer()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var firstRider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var secondRider = await PersistRiderAsync(delivery, 6.5260, 3.3810);
        var options = MatchingOptions();
        var now = Now.AddHours(2);

        using var firstScope = CreateScope();
        using var secondScope = CreateScope();
        var firstProcessor = CreateMatchingHandler(firstScope.ServiceProvider, options, now);
        var secondProcessor = CreateMatchingHandler(secondScope.ServiceProvider, options, now);

        await Task.WhenAll(
            firstProcessor.HandleAsync(new(), CancellationToken.None),
            secondProcessor.HandleAsync(new(), CancellationToken.None));

        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.DeliveryMatchingSessions.CountAsync(item =>
            item.DeliveryId == delivery.Id && item.Status == DeliveryMatchingSessionStatus.Active)).ShouldBe(1);
        var offers = await dbContext.RiderDeliveryOffers
            .Where(item => item.DeliveryId == delivery.Id && item.Status == RiderDeliveryOfferStatus.Pending)
            .ToListAsync();
        offers.Count.ShouldBe(1);
        var riderProfileIds = await dbContext.RiderProfiles
            .Where(item => item.StakeholderId == firstRider.StakeholderId || item.StakeholderId == secondRider.StakeholderId)
            .Select(item => item.Id)
            .ToListAsync();
        riderProfileIds.ShouldContain(offers[0].RiderProfileId);
    }

    [Fact]
    public async Task MakeConcurrentAndSequentialOfferAcceptanceIdempotent()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var options = MatchingOptions();
        var now = Now.AddHours(2);
        await CreateOfferAsync(delivery.Id, options, now);
        Guid offerId;
        using (var scope = CreateScope())
        {
            offerId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
                .Where(item => item.DeliveryId == delivery.Id)
                .Select(item => item.Id)
                .SingleAsync();
        }

        using var firstScope = CreateScope();
        using var secondScope = CreateScope();
        var actor = new ActorContext(rider.StakeholderId, rider.TenantId, "accept-race", "accept-race");
        var results = await Task.WhenAll(
            CreateAcceptanceHandler(firstScope.ServiceProvider, options, now.AddSeconds(1))
                .HandleAsync(new(offerId, actor), CancellationToken.None),
            CreateAcceptanceHandler(secondScope.ServiceProvider, options, now.AddSeconds(1))
                .HandleAsync(new(offerId, actor), CancellationToken.None));
        results.ShouldAllBe(result => result.Status == AcceptRiderOfferStatus.Success);

        using var replayScope = CreateScope();
        var replay = await CreateAcceptanceHandler(replayScope.ServiceProvider, options, now.AddSeconds(2))
            .HandleAsync(new(offerId, actor), CancellationToken.None);
        replay.Status.ShouldBe(AcceptRiderOfferStatus.Success);

        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await dbContext.Deliveries
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == delivery.Id);
        persisted.RiderId.ShouldBe(rider.StakeholderId);
        persisted.StatusHistory.Count(item => item.NewStatus == DeliveryStatus.RiderAssigned).ShouldBe(1);
        (await dbContext.RiderDeliveryOffers.CountAsync(item =>
            item.DeliveryId == delivery.Id && item.Status == RiderDeliveryOfferStatus.Accepted)).ShouldBe(1);
        var senderNotificationId = NotificationMessageIdentity.Create(
            delivery.Id,
            NotificationType.RiderAssigned,
            delivery.SenderId);
        var riderNotificationId = NotificationMessageIdentity.Create(
            delivery.Id,
            NotificationType.RiderAcceptedInstructions,
            rider.StakeholderId);
        (await dbContext.OutboxMessages.CountAsync(item =>
            item.MessageId == senderNotificationId || item.MessageId == riderNotificationId)).ShouldBe(2);
    }

    [Fact]
    public async Task KeepAvailabilityAndAcceptanceConsistentWhenTheyRace()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var options = MatchingOptions();
        var now = Now.AddHours(2);
        await CreateOfferAsync(delivery.Id, options, now);
        Guid offerId;
        using (var scope = CreateScope())
        {
            offerId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
                .Where(item => item.DeliveryId == delivery.Id)
                .Select(item => item.Id)
                .SingleAsync();
        }

        using var availabilityScope = CreateScope();
        using var acceptanceScope = CreateScope();
        var actor = new ActorContext(rider.StakeholderId, rider.TenantId, "availability-race", "availability-race");
        var availabilityTask = CreateAvailabilityHandler(availabilityScope.ServiceProvider, options, now.AddSeconds(1))
            .HandleAsync(new(false, actor), CancellationToken.None);
        var acceptanceTask = CreateAcceptanceHandler(acceptanceScope.ServiceProvider, options, now.AddSeconds(1))
            .HandleAsync(new(offerId, actor), CancellationToken.None);
        await Task.WhenAll(availabilityTask, acceptanceTask);

        var availabilityResult = await availabilityTask;
        var acceptanceResult = await acceptanceTask;
        availabilityResult.Status.ShouldBe(SetRiderAvailabilityStatus.Success);
        acceptanceResult.Status.ShouldBeOneOf(AcceptRiderOfferStatus.Success, AcceptRiderOfferStatus.Conflict);
        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await dbContext.RiderProfiles.SingleAsync(item => item.StakeholderId == rider.StakeholderId);
        var offer = await dbContext.RiderDeliveryOffers.SingleAsync(item => item.Id == offerId);
        var persistedDelivery = await dbContext.Deliveries.SingleAsync(item => item.Id == delivery.Id);
        profile.AvailabilityStatus.ShouldBe(RiderAvailabilityStatus.Offline);
        offer.Status.ShouldNotBe(RiderDeliveryOfferStatus.Pending);
        if (offer.Status == RiderDeliveryOfferStatus.Accepted)
        {
            persistedDelivery.RiderId.ShouldBe(rider.StakeholderId);
        }
        else
        {
            offer.Status.ShouldBe(RiderDeliveryOfferStatus.Revoked);
            persistedDelivery.RiderId.ShouldBeNull();
        }
    }

    [Fact]
    public async Task ResolveOfferExpiryAndAcceptanceRaceWithoutInvalidAssignment()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var options = MatchingOptions();
        var offeredAt = Now.AddHours(2);
        await CreateOfferAsync(delivery.Id, options, offeredAt);
        RiderDeliveryOffer offer;
        using (var scope = CreateScope())
        {
            offer = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
                .AsNoTracking()
                .SingleAsync(item => item.DeliveryId == delivery.Id);
        }

        using var acceptanceScope = CreateScope();
        using var expiryScope = CreateScope();
        var actor = new ActorContext(rider.StakeholderId, rider.TenantId, "expiry-race", "expiry-race");
        var acceptanceTask = CreateAcceptanceHandler(
                acceptanceScope.ServiceProvider,
                options,
                offer.ExpiresAtUtc.AddMilliseconds(-1))
            .HandleAsync(new(offer.Id, actor), CancellationToken.None);
        var expiryTask = CreateMatchingHandler(
                expiryScope.ServiceProvider,
                options,
                offer.ExpiresAtUtc.AddMilliseconds(1))
            .HandleAsync(new(), CancellationToken.None);
        await Task.WhenAll(acceptanceTask, expiryTask);

        var acceptanceResult = await acceptanceTask;
        acceptanceResult.Status.ShouldBeOneOf(
            AcceptRiderOfferStatus.Success,
            AcceptRiderOfferStatus.Expired,
            AcceptRiderOfferStatus.Conflict);
        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedOffer = await dbContext.RiderDeliveryOffers.SingleAsync(item => item.Id == offer.Id);
        var persistedDelivery = await dbContext.Deliveries
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == delivery.Id);
        persistedOffer.Status.ShouldBeOneOf(RiderDeliveryOfferStatus.Accepted, RiderDeliveryOfferStatus.Expired);
        persistedDelivery.StatusHistory.Count(item => item.NewStatus == DeliveryStatus.RiderAssigned)
            .ShouldBe(persistedOffer.Status == RiderDeliveryOfferStatus.Accepted ? 1 : 0);
        persistedDelivery.RiderId.ShouldBe(
            persistedOffer.Status == RiderDeliveryOfferStatus.Accepted ? rider.StakeholderId : null);
    }

    [Fact]
    public async Task RetrySerializationFailureWithCleanTrackedState()
    {
        using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var transactionFactory = services.GetRequiredService<IRiderTransactionFactory>();
        var dbContext = services.GetRequiredService<AppDbContext>();
        var attempts = 0;

        var result = await transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.Serializable,
            new("forced_serialization_retry", Guid.CreateVersion7()),
            async (transaction, cancellationToken) =>
            {
                attempts++;
                if (attempts == 1)
                {
                    dbContext.Add(Currency.Create($"R{Guid.CreateVersion7():N}"[..3], "Rolled back", true));
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "DO $$ BEGIN RAISE EXCEPTION 'forced serialization failure' USING ERRCODE = '40001'; END $$;",
                        cancellationToken);
                }

                dbContext.ChangeTracker.Entries().ShouldBeEmpty();
                await transaction.CommitAsync(cancellationToken);
                return attempts;
            },
            CancellationToken.None);

        result.ShouldBe(2);
        attempts.ShouldBe(2);
    }

    [Fact]
    public async Task DoNotRetryOrReclassifyUnrelatedUniqueViolation()
    {
        using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var transactionFactory = services.GetRequiredService<IRiderTransactionFactory>();
        var dbContext = services.GetRequiredService<AppDbContext>();
        var code = $"U{Guid.CreateVersion7():N}"[..3].ToUpperInvariant();
        dbContext.Add(Currency.Create(code, "Unique classification source", true));
        await dbContext.SaveChangesAsync();
        var attempts = 0;

        var exception = await Should.ThrowAsync<Exception>(() => transactionFactory.ExecuteAsync(
            RiderTransactionIsolation.ReadCommitted,
            new("unrelated_unique_violation", Guid.CreateVersion7()),
            async (transaction, cancellationToken) =>
            {
                attempts++;
                dbContext.Add(Currency.Create(code, "Unique classification duplicate", true));
                await transaction.CommitAsync(cancellationToken);
                return true;
            },
            CancellationToken.None));

        exception.ShouldNotBeOfType<RiderPersistenceConflictException>();
        exception.ShouldNotBeOfType<RiderTransientPersistenceException>();
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task NotOfferRiderWhoWentOfflineWhileMatchingWaitedForRiderLock()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var options = MatchingOptions();
        var now = Now.AddHours(2);

        using var blockerScope = CreateScope();
        var blocker = blockerScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var blockerTransaction = await blocker.Database.BeginTransactionAsync();
        await blockerScope.ServiceProvider.GetRequiredService<IRiderConsistencyLock>()
            .AcquireRiderAsync(rider.StakeholderId, CancellationToken.None);

        using var matchingScope = CreateScope();
        var matchingTask = CreateMatchingHandler(matchingScope.ServiceProvider, options, now)
            .HandleAsync(new(), CancellationToken.None);
        await WaitForAdvisoryLockWaiterAsync(matchingTask);

        // Same write SetRiderAvailability performs under the rider lock, committed while matching waits for it.
        await blocker.RiderProfiles
            .Where(item => item.StakeholderId == rider.StakeholderId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                item => item.AvailabilityStatus,
                RiderAvailabilityStatus.Offline));
        await blockerTransaction.CommitAsync();
        var processing = await matchingTask;

        processing.FailedOperations.ShouldBe(0);
        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.RiderDeliveryOffers.AnyAsync(item => item.DeliveryId == delivery.Id)).ShouldBeFalse();
        var session = await dbContext.DeliveryMatchingSessions.SingleAsync(item => item.DeliveryId == delivery.Id);
        session.NextAttemptAtUtc.ShouldBe(now + options.RetryBackoff);
    }

    [Fact]
    public async Task HoldOfferDeliveryLockBeforeRevokingOfferWhenRiderGoesOffline()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var options = MatchingOptions();
        var now = Now.AddHours(2);
        await CreateOfferAsync(delivery.Id, options, now);

        using var blockerScope = CreateScope();
        var blocker = blockerScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var blockerTransaction = await blocker.Database.BeginTransactionAsync();
        await blockerScope.ServiceProvider.GetRequiredService<IRiderConsistencyLock>()
            .AcquireDeliveryAsync(delivery.Id, CancellationToken.None);

        using var availabilityScope = CreateScope();
        var actor = new ActorContext(rider.StakeholderId, rider.TenantId, "offline-lock", "offline-lock");
        var availabilityTask = CreateAvailabilityHandler(availabilityScope.ServiceProvider, options, now.AddSeconds(1))
            .HandleAsync(new(false, actor), CancellationToken.None);
        await WaitForAdvisoryLockWaiterAsync(availabilityTask);

        using (var observationScope = CreateScope())
        {
            (await observationScope.ServiceProvider.GetRequiredService<AppDbContext>().RiderProfiles
                .SingleAsync(item => item.StakeholderId == rider.StakeholderId))
                .AvailabilityStatus.ShouldBe(RiderAvailabilityStatus.Available);
        }

        await blockerTransaction.RollbackAsync();
        await blockerTransaction.DisposeAsync();
        (await availabilityTask).Status.ShouldBe(SetRiderAvailabilityStatus.Success);

        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await dbContext.RiderDeliveryOffers.SingleAsync(item => item.DeliveryId == delivery.Id))
            .Status.ShouldBe(RiderDeliveryOfferStatus.Revoked);
        var session = await dbContext.DeliveryMatchingSessions.SingleAsync(item =>
            item.DeliveryId == delivery.Id && item.Status == DeliveryMatchingSessionStatus.Active);
        session.NextAttemptAtUtc.ShouldBe(now.AddSeconds(1) + options.RetryBackoff);
    }

    [Fact]
    public async Task KeepOnePendingOfferPerRiderAcrossConcurrentProcessorsAndDeliveries()
    {
        var (first, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var (second, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(first, 6.5250, 3.3800);
        await MoveDeliveryIntoRiderScopeAsync(second, first, rider.StakeholderId);
        var options = MatchingOptions();
        var now = Now.AddHours(2);

        var scopes = Enumerable.Range(0, 3).Select(_ => CreateScope()).ToArray();
        try
        {
            var results = await Task.WhenAll(scopes.Select(scope =>
                CreateMatchingHandler(scope.ServiceProvider, options, now).HandleAsync(new(), CancellationToken.None)));
            results.Sum(result => result.FailedOperations).ShouldBe(0);
        }
        finally
        {
            foreach (var scope in scopes)
            {
                scope.Dispose();
            }
        }

        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profileId = await dbContext.RiderProfiles
            .Where(item => item.StakeholderId == rider.StakeholderId)
            .Select(item => item.Id)
            .SingleAsync();
        (await dbContext.RiderDeliveryOffers.CountAsync(item =>
            item.RiderProfileId == profileId && item.Status == RiderDeliveryOfferStatus.Pending)).ShouldBe(1);
        (await dbContext.RiderDeliveryOffers.CountAsync(item =>
            (item.DeliveryId == first.Id || item.DeliveryId == second.Id) &&
            item.Status == RiderDeliveryOfferStatus.Pending)).ShouldBe(1);
        foreach (var deliveryId in new[] { first.Id, second.Id })
        {
            (await dbContext.DeliveryMatchingSessions.CountAsync(item =>
                item.DeliveryId == deliveryId && item.Status == DeliveryMatchingSessionStatus.Active)).ShouldBe(1);
        }
    }

    [Fact]
    public async Task ClassifyDuplicatePendingOfferAsBusinessConflictWithoutRetry()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var secondRider = await PersistRiderAsync(delivery, 6.5300, 3.3850);
        var options = MatchingOptions();
        var now = Now.AddHours(2);
        await CreateOfferAsync(delivery.Id, options, now);

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var existing = await dbContext.RiderDeliveryOffers.AsNoTracking().SingleAsync(item => item.DeliveryId == delivery.Id);
        var secondProfileId = await dbContext.RiderProfiles
            .Where(item => item.StakeholderId == secondRider.StakeholderId)
            .Select(item => item.Id)
            .SingleAsync();
        existing.RiderProfileId.ShouldNotBe(secondProfileId);
        var attempts = 0;

        var exception = await Should.ThrowAsync<RiderPersistenceConflictException>(() =>
            scope.ServiceProvider.GetRequiredService<IRiderTransactionFactory>().ExecuteAsync(
                RiderTransactionIsolation.ReadCommitted,
                new("duplicate_pending_offer", delivery.Id),
                async (transaction, cancellationToken) =>
                {
                    attempts++;
                    dbContext.RiderDeliveryOffers.Add(RiderDeliveryOffer.Create(
                        existing.MatchingSessionId,
                        delivery.Id,
                        secondProfileId,
                        now,
                        now + options.OfferTimeout));
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                },
                CancellationToken.None));

        exception.Message.ShouldContain("IX_RiderDeliveryOffers_DeliveryId");
        attempts.ShouldBe(1);
        (await dbContext.RiderDeliveryOffers.CountAsync(item =>
            item.DeliveryId == delivery.Id && item.Status == RiderDeliveryOfferStatus.Pending)).ShouldBe(1);
    }

    [Fact]
    public async Task SurfaceExhaustedSerializationRetriesAsTransientFailure()
    {
        using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var maximumAttempts = services.GetRequiredService<IOptions<RiderMatchingOptions>>().Value
            .MaximumTransactionRetryCount + 1;
        var attempts = 0;

        var exception = await Should.ThrowAsync<RiderTransientPersistenceException>(() =>
            services.GetRequiredService<IRiderTransactionFactory>().ExecuteAsync(
                RiderTransactionIsolation.Serializable,
                new("exhausted_serialization_retry", Guid.CreateVersion7()),
                async (transaction, cancellationToken) =>
                {
                    attempts++;
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "DO $$ BEGIN RAISE EXCEPTION 'forced serialization failure' USING ERRCODE = '40001'; END $$;",
                        cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return true;
                },
                CancellationToken.None));

        exception.Attempts.ShouldBe(maximumAttempts);
        attempts.ShouldBe(maximumAttempts);
    }

    [Theory]
    [InlineData(IneligibleRider.SwappedCoordinates)]
    [InlineData(IneligibleRider.OutsideServiceZone)]
    [InlineData(IneligibleRider.StaleLocation)]
    [InlineData(IneligibleRider.InactiveVehicleCategory)]
    [InlineData(IneligibleRider.UnapprovedProfile)]
    [InlineData(IneligibleRider.ActiveDelivery)]
    public async Task ExcludeIneligibleRiderFromPostgisCandidateSearch(IneligibleRider reason)
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var now = Now.AddHours(2);
        var swapped = reason == IneligibleRider.SwappedCoordinates;
        var rider = await PersistRiderAsync(
            delivery,
            swapped ? 3.3800 : 6.5250,
            swapped ? 6.5250 : 3.3800,
            reason == IneligibleRider.StaleLocation ? now.AddMinutes(-10) : null,
            reason == IneligibleRider.OutsideServiceZone ? await PersistUnrelatedZoneAsync() : null);

        using (var setupScope = CreateScope())
        {
            var setup = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            switch (reason)
            {
                case IneligibleRider.InactiveVehicleCategory:
                    await setup.VehicleCategories
                        .Where(item => item.Id == delivery.VehicleCategoryId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.IsActive, false));
                    break;
                case IneligibleRider.UnapprovedProfile:
                    await setup.RiderProfiles
                        .Where(item => item.StakeholderId == rider.StakeholderId)
                        .ExecuteUpdateAsync(setters => setters.SetProperty(
                            item => item.VerificationStatus,
                            RiderVerificationStatus.Pending));
                    break;
                case IneligibleRider.ActiveDelivery:
                    var (assigned, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
                    var tracked = await setup.Deliveries.SingleAsync(item => item.Id == assigned.Id);
                    tracked.AssignRider(rider.StakeholderId, now, "test-rider", "eligibility_test");
                    setup.DeliveryStatusHistory.Add(tracked.StatusHistory.Last());
                    await setup.SaveChangesAsync();
                    break;
            }
        }

        using var scope = CreateScope();
        var processing = await CreateMatchingHandler(scope.ServiceProvider, MatchingOptions(), now)
            .HandleAsync(new(), CancellationToken.None);

        processing.NoCandidateAttempts.ShouldBeGreaterThan(0);
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
            .AnyAsync(item => item.DeliveryId == delivery.Id)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ApplySearchRadiusBoundaryInMetres(bool radiusReachesRider)
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5300, 3.3792);
        double distance;
        using (var measureScope = CreateScope())
        {
            var measure = measureScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pickupPoint = new Point((double)delivery.PickupAddress.Longitude, (double)delivery.PickupAddress.Latitude)
                { SRID = 4326 };
            distance = await (from location in measure.RiderCurrentLocations
                    join profile in measure.RiderProfiles on location.RiderProfileId equals profile.Id
                    where profile.StakeholderId == rider.StakeholderId
                    select EF.Property<Point>(location, RiderCurrentLocationConfiguration.PositionProperty)
                        .Distance(pickupPoint))
                .SingleAsync();
        }

        // ~620 m north of the pickup; a degree-based (non-geography) distance would be ~0.0056.
        distance.ShouldBeInRange(500, 800);
        (distance % 1).ShouldNotBe(0);
        var radius = radiusReachesRider ? (int)Math.Ceiling(distance) : (int)Math.Floor(distance);

        using var scope = CreateScope();
        await CreateMatchingHandler(scope.ServiceProvider, MatchingOptions(radius), Now.AddHours(2))
            .HandleAsync(new(), CancellationToken.None);

        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
            .AnyAsync(item => item.DeliveryId == delivery.Id)).ShouldBe(radiusReachesRider);
    }

    [Fact]
    public async Task StoreHeartbeatLatitudeAndLongitudeInGeographyAxisOrder()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.58, 3.44);
        var options = MatchingOptions();
        var capturedAt = Now.AddHours(2).AddSeconds(1);

        using (var locationScope = CreateScope())
        {
            var services = locationScope.ServiceProvider;
            var handler = new UpdateRiderLocationHandler(
                services.GetRequiredService<RiderActorResolver>(),
                services.GetRequiredService<IRiderTransactionFactory>(),
                services.GetRequiredService<IRiderConsistencyLock>(),
                services.GetRequiredService<IRiderLocationPersistence>(),
                services.GetRequiredService<IRiderMatchingTelemetry>(),
                Options.Create(options),
                new FixedTimeProvider(capturedAt));
            var result = await handler.HandleAsync(
                new(6.5250m, 3.3800m, capturedAt, new(rider.StakeholderId, rider.TenantId, "heartbeat", "heartbeat")),
                CancellationToken.None);
            result.Status.ShouldBe(UpdateRiderLocationStatus.Success);
        }

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var position = await (from location in dbContext.RiderCurrentLocations
                join profile in dbContext.RiderProfiles on location.RiderProfileId equals profile.Id
                where profile.StakeholderId == rider.StakeholderId
                select EF.Property<Point>(location, RiderCurrentLocationConfiguration.PositionProperty))
            .SingleAsync();
        position.X.ShouldBe(3.3800, 1e-9);
        position.Y.ShouldBe(6.5250, 1e-9);

        await CreateMatchingHandler(scope.ServiceProvider, options, capturedAt.AddSeconds(1))
            .HandleAsync(new(), CancellationToken.None);
        (await dbContext.RiderDeliveryOffers.AnyAsync(item =>
            item.DeliveryId == delivery.Id && item.Status == RiderDeliveryOfferStatus.Pending)).ShouldBeTrue();
    }

    [Fact]
    public async Task ObserveDeliveryWriterOutsideRiderLocksBeforeAssigning()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var rider = await PersistRiderAsync(delivery, 6.5250, 3.3800);
        var options = MatchingOptions();
        var now = Now.AddHours(2);
        await CreateOfferAsync(delivery.Id, options, now);
        Guid offerId;
        using (var scope = CreateScope())
        {
            offerId = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
                .Where(item => item.DeliveryId == delivery.Id)
                .Select(item => item.Id)
                .SingleAsync();
        }

        // A non-rider module (e.g. cancellation) updates the Delivery without rider advisory locks.
        using var writerScope = CreateScope();
        var writer = writerScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var writerTransaction = await writer.Database.BeginTransactionAsync();
        await writer.Deliveries
            .Where(item => item.Id == delivery.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, DeliveryStatus.Cancelled));

        using var acceptanceScope = CreateScope();
        var actor = new ActorContext(rider.StakeholderId, rider.TenantId, "row-lock", "row-lock");
        var acceptanceTask = CreateAcceptanceHandler(acceptanceScope.ServiceProvider, options, now.AddSeconds(1))
            .HandleAsync(new(offerId, actor), CancellationToken.None);
        await WaitForLockWaiterAsync(acceptanceTask, "transactionid");
        await writerTransaction.CommitAsync();

        (await acceptanceTask).Status.ShouldBe(AcceptRiderOfferStatus.Conflict);
        using var assertionScope = CreateScope();
        var dbContext = assertionScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persisted = await dbContext.Deliveries
            .Include(item => item.StatusHistory)
            .SingleAsync(item => item.Id == delivery.Id);
        persisted.Status.ShouldBe(DeliveryStatus.Cancelled);
        persisted.RiderId.ShouldBeNull();
        persisted.StatusHistory.ShouldNotContain(item => item.NewStatus == DeliveryStatus.RiderAssigned);
        (await dbContext.RiderDeliveryOffers.SingleAsync(item => item.Id == offerId))
            .Status.ShouldNotBe(RiderDeliveryOfferStatus.Accepted);
    }

    public enum IneligibleRider
    {
        SwappedCoordinates = 1,
        OutsideServiceZone = 2,
        StaleLocation = 3,
        InactiveVehicleCategory = 4,
        UnapprovedProfile = 5,
        ActiveDelivery = 6
    }

    private Task WaitForAdvisoryLockWaiterAsync(Task competingOperation) =>
        WaitForLockWaiterAsync(competingOperation, "advisory");

    private async Task WaitForLockWaiterAsync(Task competingOperation, string lockType)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            if (competingOperation.IsCompleted)
            {
                await competingOperation;
                throw new ShouldAssertException($"The competing operation finished without waiting for a {lockType} lock.");
            }

            var waiters = await dbContext.Database
                .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM pg_locks WHERE locktype = {lockType} AND NOT granted")
                .SingleAsync();
            if (waiters > 0)
            {
                return;
            }

            if (stopwatch.Elapsed > TimeSpan.FromSeconds(30))
            {
                throw new TimeoutException($"The competing operation never waited for a {lockType} lock.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(20));
        }
    }

    private async Task MoveDeliveryIntoRiderScopeAsync(Delivery target, Delivery source, Guid riderStakeholderId)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Deliveries
            .Where(item => item.Id == target.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.SenderId, source.SenderId)
                .SetProperty(item => item.VehicleCategoryId, source.VehicleCategoryId));
        var targetZoneId = await dbContext.DeliveryQuotes
            .Where(item => item.Id == target.DeliveryQuoteId)
            .Select(item => item.OriginZoneId)
            .SingleAsync();
        var profileId = await dbContext.RiderProfiles
            .Where(item => item.StakeholderId == riderStakeholderId)
            .Select(item => item.Id)
            .SingleAsync();
        dbContext.RiderServiceZones.Add(RiderServiceZone.Create(profileId, targetZoneId));
        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> PersistUnrelatedZoneAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var zone = DeliveryZone.Create($"O{Guid.CreateVersion7():N}"[..9], "Unrelated zone", "NG", 1, Now.AddDays(-1));
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var ring = factory.CreateLinearRing(
            [new(7.00, 9.00), new(7.10, 9.00), new(7.10, 9.10), new(7.00, 9.10), new(7.00, 9.00)]);
        dbContext.Add(zone);
        dbContext.Entry(zone).Property<MultiPolygon>(DeliveryZoneConfiguration.BoundaryProperty)
            .CurrentValue = factory.CreateMultiPolygon([factory.CreatePolygon(ring)]);
        await dbContext.SaveChangesAsync();
        return zone.Id;
    }

    private async Task CreateOfferAsync(
        Guid deliveryId,
        RiderMatchingOptions options,
        DateTimeOffset utcNow)
    {
        using var scope = CreateScope();
        var result = await CreateMatchingHandler(scope.ServiceProvider, options, utcNow)
            .HandleAsync(new(), CancellationToken.None);
        result.FailedOperations.ShouldBe(0);
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
            .AnyAsync(item => item.DeliveryId == deliveryId && item.Status == RiderDeliveryOfferStatus.Pending))
            .ShouldBeTrue();
    }

    private async Task<(Guid StakeholderId, Guid TenantId)> PersistRiderAsync(
        Delivery delivery,
        double latitude,
        double longitude,
        DateTimeOffset? locationCapturedAtUtc = null,
        Guid? serviceZoneId = null)
    {
        using var scope = CreateScope();
        var services = scope.ServiceProvider;
        var dbContext = services.GetRequiredService<AppDbContext>();
        var identityService = services.GetRequiredService<IAuthenticationIdentityService>();
        var sender = await dbContext.Stakeholders.AsNoTracking().SingleAsync(item => item.Id == delivery.SenderId);
        var quote = await dbContext.DeliveryQuotes.AsNoTracking().SingleAsync(item => item.Id == delivery.DeliveryQuoteId);
        var suffix = Guid.CreateVersion7().ToString("N")[^8..];
        var user = AppUser.Create($"rider-{suffix}@example.com");
        (await identityService.CreateAsync(user, "P@ssw0rd123!")).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = await dbContext.StakeholderTypes.SingleOrDefaultAsync(
            item => item.TenantId == sender.TenantId && item.Key == "rider");
        stakeholderType ??= StakeholderType.Create(sender.TenantId, "Rider", "rider");
        var stakeholder = Stakeholder.Create(
            user.Id,
            sender.TenantId,
            sender.CountryId,
            stakeholderType.Id,
            "Test",
            "Rider");
        stakeholder.MarkVerified();
        var profile = RiderProfile.Create(stakeholder.Id, sender.TenantId, delivery.VehicleCategoryId);
        profile.Approve();
        profile.SetAvailability(true);
        var location = RiderCurrentLocation.Create(
            profile.Id,
            locationCapturedAtUtc ?? Now.AddHours(2),
            locationCapturedAtUtc ?? Now.AddHours(2));

        if (dbContext.Entry(stakeholderType).State == EntityState.Detached)
        {
            dbContext.Add(stakeholderType);
        }

        dbContext.AddRange(stakeholder, profile, location, RiderServiceZone.Create(profile.Id, serviceZoneId ?? quote.OriginZoneId));
        dbContext.Entry(location).Property<Point>(RiderCurrentLocationConfiguration.PositionProperty)
            .CurrentValue = new Point(longitude, latitude) { SRID = 4326 };
        await dbContext.SaveChangesAsync();
        return (stakeholder.Id, sender.TenantId);
    }

    private static RiderMatchingOptions MatchingOptions(int initialSearchRadiusMeters = 3000) => new()
    {
        LocationFreshness = TimeSpan.FromMinutes(5),
        MaximumFutureLocationClockSkew = TimeSpan.FromSeconds(30),
        InitialSearchRadiusMeters = initialSearchRadiusMeters,
        MaximumSearchRadiusMeters = 15000,
        SearchRadiusExpansionMeters = 3000,
        MaximumCandidatesPerRound = 20,
        OfferTimeout = TimeSpan.FromSeconds(30),
        MaximumAttemptsPerCycle = 5,
        RetryBackoff = TimeSpan.FromSeconds(30),
        MatchingScanBatchSize = 500,
        ScheduledDispatchScanBatchSize = 500,
        MaximumConcurrentAssignmentsPerRider = 1
    };
}
