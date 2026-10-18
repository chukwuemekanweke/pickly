using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Exceptions;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.Infrastructure.Riders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetTopologySuite.Geometries;
using Npgsql;
using System.Data;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

internal sealed class RiderOfferAcceptancePersistence(
    AppDbContext dbContext,
    RiderPersistenceSupport support) : IRiderOfferAcceptancePersistence
{
    public async Task<RiderOfferDecisionResult> AcceptAsync(
        ActorContext actor,
        Guid offerId,
        DateTimeOffset utcNow,
        RiderMatchingOptions options,
        CancellationToken cancellationToken)
    {
        var resolved = await support.ResolveRiderAsync(actor, cancellationToken);
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
            await support.AcquireAssignmentLocksAsync(offerPreview.DeliveryId, riderStakeholderId, cancellationToken);
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
                return new(RiderOperationCode.Success, RiderPersistenceSupport.CreateAssignedDeliverySnapshot(delivery));
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
                !RiderPersistenceSupport.IsFresh(location, utcNow, options) || profile.VehicleCategoryId != delivery.VehicleCategoryId ||
                !await support.IsVehicleCategoryActiveAsync(profile.VehicleCategoryId, cancellationToken) ||
                !geographicallyEligible || !delivery.IsMatchingEligible ||
                await support.HasActiveDeliveryAsync(profile.StakeholderId, cancellationToken))
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
            return new(RiderOperationCode.Success, RiderPersistenceSupport.CreateAssignedDeliverySnapshot(delivery));
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return await RollBackAsConflictAsync(transaction, cancellationToken);
        }
        catch (AggregateStateException)
        {
            return await RollBackAsConflictAsync(transaction, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return await RollBackAsConflictAsync(transaction, cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            return await RollBackAsConflictAsync(transaction, cancellationToken);
        }
    }

    private async Task<RiderOfferDecisionResult> RollBackAsConflictAsync(
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        dbContext.ChangeTracker.Clear();
        RiderMatchingMetrics.RecordAssignmentConflict();
        return new(RiderOperationCode.Conflict);
    }

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

        var pickupPoint = RiderPersistenceSupport.CreatePoint(delivery.PickupAddress.Latitude, delivery.PickupAddress.Longitude);
        var zoneId = await support.FindPickupZoneIdAsync(delivery, pickupPoint, utcNow, cancellationToken);
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
}
