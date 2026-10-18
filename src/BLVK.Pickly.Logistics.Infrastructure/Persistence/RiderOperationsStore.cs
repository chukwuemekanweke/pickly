using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.Infrastructure.Riders;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence;

public sealed class RiderOperationsStore(AppDbContext dbContext) : IRiderOperationsStore
{
    private const string RiderStakeholderTypeKey = "rider";
    private const string MatchingProcessorLockKey = "rider-matching-processor";
    private static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<RiderOperationResult> UpdateLocationAsync(
        ActorContext actor,
        decimal latitude,
        decimal longitude,
        DateTimeOffset capturedAtUtc,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        if (latitude is < -90m or > 90m || longitude is < -180m or > 180m)
        {
            return new(RiderOperationCode.InvalidLocation);
        }

        var captured = capturedAtUtc.ToUniversalTime();
        var now = utcNow.ToUniversalTime();
        if (captured > now + options.MaximumFutureLocationClockSkew)
        {
            return new(RiderOperationCode.InvalidLocation);
        }

        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var profile = resolved.Profile!;
        await AcquireKeyLockAsync($"rider:{profile.StakeholderId:D}", cancellationToken);
        await dbContext.Entry(profile).ReloadAsync(cancellationToken);
        if (!profile.IsApproved)
        {
            return new(RiderOperationCode.ProfileInactive);
        }
        var location = await dbContext.RiderCurrentLocations
            .SingleOrDefaultAsync(item => item.RiderProfileId == profile.Id, cancellationToken);
        var point = CreatePoint(latitude, longitude);

        if (location is null)
        {
            location = RiderCurrentLocation.Create(profile.Id, captured, now);
            dbContext.RiderCurrentLocations.Add(location);
            dbContext.Entry(location).Property<Point>(RiderCurrentLocationConfiguration.PositionProperty).CurrentValue = point;
        }
        else
        {
            if (!location.TryAdvance(captured, now))
            {
                RiderMatchingMetrics.RecordStaleLocation();
                return new(RiderOperationCode.StaleLocation);
            }

            dbContext.Entry(location).Property<Point>(RiderCurrentLocationConfiguration.PositionProperty).CurrentValue = point;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(RiderOperationCode.Success);
    }

    public async Task<RiderStatusResult> SetAvailabilityAsync(
        ActorContext actor,
        bool isAvailable,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var profile = resolved.Profile!;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await AcquireKeyLockAsync($"rider:{profile.StakeholderId:D}", cancellationToken);
        await dbContext.Entry(profile).ReloadAsync(cancellationToken);
        if (!profile.IsApproved)
        {
            return new(RiderOperationCode.ProfileInactive);
        }

        var hasActiveDelivery = await HasActiveDeliveryAsync(profile.StakeholderId, cancellationToken);
        if (isAvailable && hasActiveDelivery)
        {
            return new(RiderOperationCode.Conflict);
        }

        if (isAvailable && !await IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, cancellationToken))
        {
            return new(RiderOperationCode.Conflict);
        }

        var location = await dbContext.RiderCurrentLocations
            .SingleOrDefaultAsync(item => item.RiderProfileId == profile.Id, cancellationToken);
        if (isAvailable && !IsFresh(location, utcNow, options))
        {
            return new(RiderOperationCode.StaleLocation);
        }

        profile.SetAvailability(isAvailable);
        if (!isAvailable)
        {
            var offers = await dbContext.RiderDeliveryOffers
                .Where(offer => offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
                .ToListAsync(cancellationToken);
            foreach (var offer in offers)
            {
                offer.Revoke(utcNow);
                var session = await dbContext.DeliveryMatchingSessions
                    .SingleOrDefaultAsync(item => item.Id == offer.MatchingSessionId, cancellationToken);
                if (session?.Status == DeliveryMatchingSessionStatus.Active)
                {
                    session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await BuildStatusResultAsync(profile, location, utcNow, options, cancellationToken);
    }

    public async Task<RiderStatusResult> GetStatusAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var location = await dbContext.RiderCurrentLocations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.RiderProfileId == resolved.Profile!.Id, cancellationToken);
        return await BuildStatusResultAsync(resolved.Profile!, location, utcNow, options, cancellationToken);
    }

    public async Task<RiderOffersResult> ListOffersAsync(
        ActorContext actor,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        if (!resolved.Profile!.IsApproved)
        {
            return new(RiderOperationCode.ProfileInactive);
        }

        var expired = await dbContext.RiderDeliveryOffers
            .Where(offer =>
                offer.RiderProfileId == resolved.Profile.Id &&
                offer.Status == RiderDeliveryOfferStatus.Pending &&
                offer.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
        foreach (var offer in expired)
        {
            offer.Expire(utcNow);
        }

        if (expired.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var offers = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .Where(offer =>
                offer.RiderProfileId == resolved.Profile.Id &&
                offer.Status == RiderDeliveryOfferStatus.Pending &&
                offer.ExpiresAtUtc > utcNow)
            .OrderBy(offer => offer.ExpiresAtUtc)
            .ThenBy(offer => offer.Id)
            .ToListAsync(cancellationToken);
        var deliveryIds = offers.Select(offer => offer.DeliveryId).ToArray();
        var deliveries = await dbContext.Deliveries
            .AsNoTracking()
            .Include(delivery => delivery.Items)
            .Where(delivery => deliveryIds.Contains(delivery.Id))
            .ToDictionaryAsync(delivery => delivery.Id, cancellationToken);

        var snapshots = offers
            .Where(offer => deliveries.ContainsKey(offer.DeliveryId))
            .Select(offer =>
            {
                var delivery = deliveries[offer.DeliveryId];
                return new RiderOfferSnapshot(
                    offer.Id,
                    delivery.Id,
                    delivery.TrackingId,
                    delivery.PickupAddress.AddressLine,
                    $"{delivery.DropOffAddress.City}, {delivery.DropOffAddress.State}",
                    delivery.VehicleCategoryId,
                    delivery.Items.Sum(item => item.Quantity),
                    delivery.Items.Sum(item => item.Quantity * item.WeightKg),
                    offer.ExpiresAtUtc);
            })
            .ToArray();

        return new(RiderOperationCode.Success, snapshots);
    }

    public async Task<RiderOfferDecisionResult> AcceptOfferAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var offerPreview = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (offerPreview is null)
        {
            return new(RiderOperationCode.NotFound);
        }

        if (offerPreview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(RiderOperationCode.Forbidden);
        }

        var profileId = resolved.Profile.Id;
        var riderStakeholderId = resolved.Profile.StakeholderId;
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            await AcquireAssignmentLocksAsync(offerPreview.DeliveryId, riderStakeholderId, cancellationToken);
            var offer = await dbContext.RiderDeliveryOffers
                .SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);
            if (offer is null)
            {
                return new(RiderOperationCode.NotFound);
            }

            var profile = await dbContext.RiderProfiles
                .SingleAsync(item => item.Id == profileId, cancellationToken);
            if (offer.RiderProfileId != profile.Id)
            {
                return new(RiderOperationCode.Forbidden);
            }
            var delivery = await dbContext.Deliveries
                .Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.Id == offer.DeliveryId, cancellationToken);
            if (delivery is null)
            {
                return new(RiderOperationCode.NotFound);
            }

            if (offer.Status == RiderDeliveryOfferStatus.Accepted && delivery.RiderId == profile.StakeholderId)
            {
                await transaction.CommitAsync(cancellationToken);
                return new(RiderOperationCode.Success, CreateAssignedDeliverySnapshot(delivery));
            }

            if (offer.Status != RiderDeliveryOfferStatus.Pending)
            {
                return new(offer.Status == RiderDeliveryOfferStatus.Expired
                    ? RiderOperationCode.Expired
                    : RiderOperationCode.Conflict);
            }

            if (utcNow >= offer.ExpiresAtUtc)
            {
                offer.Expire(utcNow);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new(RiderOperationCode.Expired);
            }

            var session = await dbContext.DeliveryMatchingSessions
                .SingleAsync(item => item.Id == offer.MatchingSessionId, cancellationToken);
            var location = await dbContext.RiderCurrentLocations
                .SingleOrDefaultAsync(item => item.RiderProfileId == profile.Id, cancellationToken);
            var geographicallyEligible = await IsGeographicallyEligibleAsync(
                profile,
                delivery,
                location,
                session.SearchRadiusMeters,
                utcNow,
                cancellationToken);
            if (!profile.IsApproved || !profile.HasEnabledAvailability ||
                !IsFresh(location, utcNow, options) || profile.VehicleCategoryId != delivery.VehicleCategoryId ||
                !await IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, cancellationToken) ||
                !geographicallyEligible || !delivery.IsMatchingEligible ||
                await HasActiveDeliveryAsync(profile.StakeholderId, cancellationToken))
            {
                RiderMatchingMetrics.RecordAssignmentConflict();
                return new(RiderOperationCode.Conflict);
            }

            offer.Accept(utcNow);
            delivery.AssignRider(profile.StakeholderId, utcNow, profile.StakeholderId.ToString(), "rider_offer_acceptance");
            dbContext.DeliveryStatusHistory.Add(delivery.StatusHistory.Last());
            session.Complete(utcNow);

            var conflictingOffers = await dbContext.RiderDeliveryOffers
                .Where(item => item.Id != offer.Id && item.Status == RiderDeliveryOfferStatus.Pending &&
                    (item.DeliveryId == delivery.Id || item.RiderProfileId == profile.Id))
                .ToListAsync(cancellationToken);
            foreach (var conflictingOffer in conflictingOffers)
            {
                conflictingOffer.Revoke(utcNow);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            RiderMatchingMetrics.RecordOfferAccepted();
            return new(RiderOperationCode.Success, CreateAssignedDeliverySnapshot(delivery));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            RiderMatchingMetrics.RecordAssignmentConflict();
            return new(RiderOperationCode.Conflict);
        }
        catch (AggregateStateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            RiderMatchingMetrics.RecordAssignmentConflict();
            return new(RiderOperationCode.Conflict);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            RiderMatchingMetrics.RecordAssignmentConflict();
            return new(RiderOperationCode.Conflict);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            RiderMatchingMetrics.RecordAssignmentConflict();
            return new(RiderOperationCode.Conflict);
        }
    }

    public async Task<RiderOperationResult> RejectOfferAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var preview = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (preview is null)
        {
            return new(RiderOperationCode.NotFound);
        }

        if (preview.RiderProfileId != resolved.Profile!.Id)
        {
            return new(RiderOperationCode.Forbidden);
        }

        var profile = resolved.Profile!;
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await AcquireAssignmentLocksAsync(preview.DeliveryId, profile.StakeholderId, cancellationToken);
        var offer = await dbContext.RiderDeliveryOffers.SingleOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (offer is null)
        {
            return new(RiderOperationCode.NotFound);
        }

        if (offer.Status == RiderDeliveryOfferStatus.Rejected)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(RiderOperationCode.Success);
        }

        if (offer.Status != RiderDeliveryOfferStatus.Pending)
        {
            return new(offer.Status == RiderDeliveryOfferStatus.Expired
                ? RiderOperationCode.Expired
                : RiderOperationCode.Conflict);
        }

        if (utcNow >= offer.ExpiresAtUtc)
        {
            offer.Expire(utcNow);
        }
        else
        {
            offer.Reject(utcNow);
        }

        var session = await dbContext.DeliveryMatchingSessions
            .SingleOrDefaultAsync(item => item.Id == offer.MatchingSessionId, cancellationToken);
        if (session?.Status == DeliveryMatchingSessionStatus.Active)
        {
            session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        if (offer.Status == RiderDeliveryOfferStatus.Rejected)
        {
            RiderMatchingMetrics.RecordOfferRejected();
        }
        return new(offer.Status == RiderDeliveryOfferStatus.Expired
            ? RiderOperationCode.Expired
            : RiderOperationCode.Success);
    }

    public async Task<RiderActiveDeliveryResult> GetActiveDeliveryAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        var resolved = await ResolveRiderAsync(actor, cancellationToken);
        if (resolved.Code != RiderOperationCode.Success)
        {
            return new(resolved.Code);
        }

        var delivery = await dbContext.Deliveries
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item =>
                item.RiderId == resolved.Profile!.StakeholderId && ActiveDeliveryStatuses.Contains(item.Status),
                cancellationToken);
        return delivery is null
            ? new(RiderOperationCode.NotFound)
            : new(RiderOperationCode.Success, CreateAssignedDeliverySnapshot(delivery));
    }

    public async Task<SenderAssignmentSnapshot> GetSenderAssignmentAsync(
        Guid deliveryId,
        CancellationToken cancellationToken)
    {
        var delivery = await dbContext.Deliveries
            .AsNoTracking()
            .SingleAsync(item => item.Id == deliveryId, cancellationToken);
        var activeOffer = await dbContext.RiderDeliveryOffers
            .AsNoTracking()
            .AnyAsync(item => item.DeliveryId == deliveryId && item.Status == RiderDeliveryOfferStatus.Pending, cancellationToken);
        var activeSession = await dbContext.DeliveryMatchingSessions
            .AsNoTracking()
            .AnyAsync(item => item.DeliveryId == deliveryId && item.Status == DeliveryMatchingSessionStatus.Active, cancellationToken);

        string? riderName = null;
        string? riderAvatarUrl = null;
        DateTimeOffset? assignedAtUtc = null;
        if (delivery.RiderId.HasValue)
        {
            var rider = await dbContext.Stakeholders
                .AsNoTracking()
                .Where(item => item.Id == delivery.RiderId.Value)
                .Select(item => new { item.FirstName, item.LastName, item.AvatarUrl })
                .SingleOrDefaultAsync(cancellationToken);
            if (rider is not null)
            {
                riderName = $"{rider.FirstName} {rider.LastName}".Trim();
                riderAvatarUrl = rider.AvatarUrl;
            }

            assignedAtUtc = await dbContext.DeliveryStatusHistory
                .AsNoTracking()
                .Where(item => item.DeliveryId == deliveryId && item.NewStatus == DeliveryStatus.RiderAssigned)
                .OrderByDescending(item => item.OccurredAtUtc)
                .Select(item => (DateTimeOffset?)item.OccurredAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var matchingStatus = delivery.RiderId.HasValue
            ? "Assigned"
            : activeOffer
                ? "OfferActive"
                : activeSession || delivery.IsMatchingEligible
                    ? "Pending"
                    : "NotEligible";
        return new(matchingStatus, activeOffer, delivery.RiderId, riderName, riderAvatarUrl, assignedAtUtc);
    }

    public async Task<MatchingProcessingResult> ProcessMatchingAsync(
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({MatchingProcessorLockKey}, 0))",
            cancellationToken);

        var expiredOffers = await dbContext.RiderDeliveryOffers
            .Where(offer => offer.Status == RiderDeliveryOfferStatus.Pending && offer.ExpiresAtUtc <= utcNow)
            .OrderBy(offer => offer.ExpiresAtUtc)
            .Take(options.MatchingScanBatchSize)
            .ToListAsync(cancellationToken);
        foreach (var offer in expiredOffers)
        {
            offer.Expire(utcNow);
            var session = await dbContext.DeliveryMatchingSessions
                .SingleOrDefaultAsync(item => item.Id == offer.MatchingSessionId, cancellationToken);
            if (session?.Status == DeliveryMatchingSessionStatus.Active)
            {
                session.RecordAttempt(utcNow, utcNow + options.RetryBackoff, session.SearchRadiusMeters);
            }
        }

        var scheduledDeliveries = await dbContext.Deliveries
            .Where(delivery =>
                delivery.Status == DeliveryStatus.AwaitingDispatch &&
                delivery.DispatchEligibleAtUtc <= utcNow &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery || delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.DispatchEligibleAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Take(options.ScheduledDispatchScanBatchSize)
            .ToListAsync(cancellationToken);
        foreach (var delivery in scheduledDeliveries)
        {
            delivery.OpenDispatch(utcNow, "system", "scheduled_dispatch_job");
            dbContext.DeliveryStatusHistory.Add(delivery.StatusHistory.Last());
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var deliveries = await dbContext.Deliveries
            .Where(delivery =>
                delivery.Status == DeliveryStatus.PendingAssignment &&
                delivery.RiderId == null &&
                (delivery.OrderType == DeliveryOrderType.PayOnDelivery || delivery.PaymentStatus == DeliveryPaymentStatus.Paid))
            .OrderBy(delivery => delivery.CreatedAtUtc)
            .ThenBy(delivery => delivery.Id)
            .Take(options.MatchingScanBatchSize)
            .ToListAsync(cancellationToken);

        var attempts = 0;
        var created = 0;
        var noCandidates = 0;
        foreach (var delivery in deliveries)
        {
            var session = await dbContext.DeliveryMatchingSessions
                .SingleOrDefaultAsync(item =>
                    item.DeliveryId == delivery.Id && item.Status == DeliveryMatchingSessionStatus.Active,
                    cancellationToken);
            if (session is null)
            {
                session = DeliveryMatchingSession.Create(delivery.Id, options.InitialSearchRadiusMeters, utcNow);
                dbContext.DeliveryMatchingSessions.Add(session);
            }

            if (session.NextAttemptAtUtc > utcNow || await dbContext.RiderDeliveryOffers.AnyAsync(
                offer => offer.DeliveryId == delivery.Id && offer.Status == RiderDeliveryOfferStatus.Pending,
                cancellationToken))
            {
                continue;
            }

            attempts++;
            var pickupPoint = CreatePoint(delivery.PickupAddress.Latitude, delivery.PickupAddress.Longitude);
            var zoneId = await FindPickupZoneIdAsync(delivery, pickupPoint, utcNow, cancellationToken);
            RiderCandidate? candidate = null;
            if (zoneId.HasValue)
            {
                var freshAfter = utcNow.ToUniversalTime() - options.LocationFreshness;
                candidate = await (
                    from profile in dbContext.RiderProfiles
                    join location in dbContext.RiderCurrentLocations on profile.Id equals location.RiderProfileId
                    join serviceZone in dbContext.RiderServiceZones on profile.Id equals serviceZone.RiderProfileId
                    join stakeholder in dbContext.Stakeholders on profile.StakeholderId equals stakeholder.Id
                    join stakeholderType in dbContext.StakeholderTypes on stakeholder.StakeholderTypeId equals stakeholderType.Id
                    join vehicle in dbContext.VehicleCategories on profile.VehicleCategoryId equals vehicle.Id
                    where profile.TenantId == dbContext.Stakeholders
                        .Where(stakeholder => stakeholder.Id == delivery.SenderId)
                        .Select(stakeholder => stakeholder.TenantId)
                        .Single()
                    where profile.VerificationStatus == RiderVerificationStatus.Approved
                    where profile.AvailabilityStatus == RiderAvailabilityStatus.Available
                    where profile.VehicleCategoryId == delivery.VehicleCategoryId
                    where vehicle.IsActive
                    where stakeholder.IsVerified && stakeholderType.Key == RiderStakeholderTypeKey
                    where location.CapturedAtUtc >= freshAfter
                    where serviceZone.DeliveryZoneId == zoneId.Value
                    where !dbContext.Deliveries.Any(item =>
                        item.RiderId == profile.StakeholderId && ActiveDeliveryStatuses.Contains(item.Status))
                    where !dbContext.RiderDeliveryOffers.Any(offer =>
                        offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending)
                    let distance = EF.Property<Point>(location, RiderCurrentLocationConfiguration.PositionProperty)
                        .Distance(pickupPoint)
                    where distance <= session.SearchRadiusMeters
                    orderby distance, profile.Id
                    select new RiderCandidate(profile.Id, distance))
                    .Take(options.MaximumCandidatesPerRound)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            var nextRadius = Math.Min(
                options.MaximumSearchRadiusMeters,
                session.SearchRadiusMeters + options.SearchRadiusExpansionMeters);
            var cycleBoundary = (session.AttemptCount + 1) % options.MaximumAttemptsPerCycle == 0;
            session.RecordAttempt(
                utcNow,
                candidate is null || cycleBoundary ? utcNow + options.RetryBackoff : utcNow,
                cycleBoundary ? options.InitialSearchRadiusMeters : nextRadius);

            if (candidate is null)
            {
                noCandidates++;
                continue;
            }

            dbContext.RiderDeliveryOffers.Add(RiderDeliveryOffer.Create(
                session.Id,
                delivery.Id,
                candidate.RiderProfileId,
                utcNow,
                utcNow + options.OfferTimeout));
            created++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var result = new MatchingProcessingResult(
            scheduledDeliveries.Count,
            expiredOffers.Count,
            attempts,
            created,
            noCandidates);
        RiderMatchingMetrics.RecordProcessing(result);
        return result;
    }

    private async Task<(RiderOperationCode Code, RiderProfile? Profile)> ResolveRiderAsync(
        ActorContext actor,
        CancellationToken cancellationToken)
    {
        if (!actor.StakeholderId.HasValue || actor.StakeholderId == Guid.Empty)
        {
            return (RiderOperationCode.NotAuthenticated, null);
        }

        var stakeholder = await dbContext.Stakeholders
            .AsNoTracking()
            .Include(item => item.StakeholderType)
            .SingleOrDefaultAsync(item => item.Id == actor.StakeholderId.Value, cancellationToken);
        if (stakeholder is null || !stakeholder.IsVerified ||
            !string.Equals(stakeholder.StakeholderType.Key, RiderStakeholderTypeKey, StringComparison.OrdinalIgnoreCase) ||
            (actor.TenantId.HasValue && actor.TenantId != stakeholder.TenantId))
        {
            return (RiderOperationCode.NotRider, null);
        }

        var profile = await dbContext.RiderProfiles
            .SingleOrDefaultAsync(item => item.StakeholderId == stakeholder.Id && item.TenantId == stakeholder.TenantId, cancellationToken);
        return profile is null
            ? (RiderOperationCode.NotRider, null)
            : (RiderOperationCode.Success, profile);
    }

    private async Task<RiderStatusResult> BuildStatusResultAsync(
        RiderProfile profile,
        RiderCurrentLocation? location,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var active = await HasActiveDeliveryAsync(profile.StakeholderId, cancellationToken);
        var pendingOffers = await dbContext.RiderDeliveryOffers.CountAsync(
            offer => offer.RiderProfileId == profile.Id && offer.Status == RiderDeliveryOfferStatus.Pending && offer.ExpiresAtUtc > utcNow,
            cancellationToken);
        var fresh = IsFresh(location, utcNow, options);
        var operationalStatus = !profile.IsApproved
            ? profile.VerificationStatus.ToString()
            : active
                ? "Busy"
                : !profile.HasEnabledAvailability
                    ? profile.AvailabilityStatus.ToString()
                    : fresh
                        ? "Available"
                        : "StaleLocation";

        return new(
            RiderOperationCode.Success,
            new(
                profile.Id,
                profile.VehicleCategoryId,
                profile.VerificationStatus.ToString(),
                operationalStatus,
                profile.HasEnabledAvailability,
                fresh,
                location?.CapturedAtUtc,
                active,
                pendingOffers));
    }

    private Task<bool> HasActiveDeliveryAsync(Guid stakeholderId, CancellationToken cancellationToken) =>
        dbContext.Deliveries.AnyAsync(
            delivery => delivery.RiderId == stakeholderId && ActiveDeliveryStatuses.Contains(delivery.Status),
            cancellationToken);

    private Task<bool> IsVehicleCategoryActiveAsync(Guid vehicleCategoryId, CancellationToken cancellationToken) =>
        dbContext.VehicleCategories.AnyAsync(
            vehicle => vehicle.Id == vehicleCategoryId && vehicle.IsActive,
            cancellationToken);

    private async Task<bool> IsGeographicallyEligibleAsync(
        RiderProfile profile,
        Delivery delivery,
        RiderCurrentLocation? location,
        int searchRadiusMeters,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (location is null)
        {
            return false;
        }

        var pickupPoint = CreatePoint(delivery.PickupAddress.Latitude, delivery.PickupAddress.Longitude);
        var zoneId = await FindPickupZoneIdAsync(delivery, pickupPoint, utcNow, cancellationToken);
        if (!zoneId.HasValue || !await dbContext.RiderServiceZones.AnyAsync(
            item => item.RiderProfileId == profile.Id && item.DeliveryZoneId == zoneId.Value,
            cancellationToken))
        {
            return false;
        }

        return await dbContext.RiderCurrentLocations.AnyAsync(
            item => item.Id == location.Id &&
                EF.Property<Point>(item, RiderCurrentLocationConfiguration.PositionProperty).Distance(pickupPoint) <= searchRadiusMeters,
            cancellationToken);
    }

    private async Task<Guid?> FindPickupZoneIdAsync(
        Delivery delivery,
        Point pickupPoint,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var quotedOriginZoneId = await dbContext.DeliveryQuotes
            .Where(quote => quote.Id == delivery.DeliveryQuoteId)
            .Select(quote => (Guid?)quote.OriginZoneId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!quotedOriginZoneId.HasValue)
        {
            return null;
        }

        var isStillSupported = await dbContext.DeliveryZones.AnyAsync(
            zone =>
                zone.Id == quotedOriginZoneId.Value &&
                zone.IsActive &&
                zone.CountryCode == delivery.PickupAddress.CountryCode &&
                zone.EffectiveFromUtc <= utcNow &&
                (zone.EffectiveToUtc == null || zone.EffectiveToUtc > utcNow) &&
                EF.Property<MultiPolygon>(zone, DeliveryZoneConfiguration.BoundaryProperty).Covers(pickupPoint),
            cancellationToken);
        return isStillSupported ? quotedOriginZoneId : null;
    }

    private async Task AcquireAssignmentLocksAsync(
        Guid deliveryId,
        Guid riderStakeholderId,
        CancellationToken cancellationToken)
    {
        var keys = new[] { $"delivery:{deliveryId:D}", $"rider:{riderStakeholderId:D}" }
            .OrderBy(value => value, StringComparer.Ordinal);
        foreach (var key in keys)
        {
            await AcquireKeyLockAsync(key, cancellationToken);
        }
    }

    private Task<int> AcquireKeyLockAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            cancellationToken);

    private static bool IsFresh(
        RiderCurrentLocation? location,
        DateTimeOffset utcNow,
        RiderMatchingOptions options) =>
        location is not null && location.CapturedAtUtc >= utcNow.ToUniversalTime() - options.LocationFreshness;

    private static Point CreatePoint(decimal latitude, decimal longitude) =>
        new((double)longitude, (double)latitude) { SRID = 4326 };

    private static RiderAssignedDeliverySnapshot CreateAssignedDeliverySnapshot(Delivery delivery) =>
        new(
            delivery.Id,
            delivery.TrackingId,
            delivery.Status.ToString(),
            delivery.DeliveryType.ToString(),
            delivery.OrderType.ToString(),
            delivery.PaymentMethod.ToString(),
            delivery.PaymentResponsibleParty.ToString(),
            delivery.PickupAddress.AddressLine,
            delivery.PickupAddress.Latitude,
            delivery.PickupAddress.Longitude,
            delivery.DropOffAddress.AddressLine,
            delivery.DropOffAddress.Latitude,
            delivery.DropOffAddress.Longitude,
            delivery.Recipient.Name,
            delivery.Recipient.PhoneNumber,
            delivery.Items.Select(item => new RiderDeliveryItemSnapshot(item.Description, item.Quantity, item.WeightKg)).ToArray());

    private sealed record RiderCandidate(Guid RiderProfileId, double DistanceMeters);
}
