using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BLVK.Pickly.Logistics.Infrastructure.Persistence.Riders;

/// <summary>
/// Rider resolution, eligibility and locking primitives shared by the purpose-specific rider persistence
/// components. It shares the scoped <see cref="AppDbContext"/> so tracked state and transactions stay consistent.
/// </summary>
internal sealed class RiderPersistenceSupport(AppDbContext dbContext)
{
    public const string RiderStakeholderTypeKey = "rider";

    public static readonly DeliveryStatus[] ActiveDeliveryStatuses =
    [
        DeliveryStatus.RiderAssigned,
        DeliveryStatus.PickedUp,
        DeliveryStatus.InTransit
    ];

    public async Task<(RiderOperationCode Code, RiderProfile? Profile)> ResolveRiderAsync(
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

    public async Task<RiderStatusResult> BuildStatusResultAsync(
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

    public Task<bool> HasActiveDeliveryAsync(Guid stakeholderId, CancellationToken cancellationToken) =>
        dbContext.Deliveries.AnyAsync(
            delivery => delivery.RiderId == stakeholderId && ActiveDeliveryStatuses.Contains(delivery.Status),
            cancellationToken);

    public Task<bool> IsVehicleCategoryActiveAsync(Guid vehicleCategoryId, CancellationToken cancellationToken) =>
        dbContext.VehicleCategories.AnyAsync(
            vehicle => vehicle.Id == vehicleCategoryId && vehicle.IsActive,
            cancellationToken);

    public async Task<Guid?> FindPickupZoneIdAsync(
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

    /// <summary>Acquires the Delivery and rider advisory locks in a stable order to avoid deadlocks.</summary>
    public async Task AcquireAssignmentLocksAsync(
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

    public Task AcquireRiderLockAsync(Guid riderStakeholderId, CancellationToken cancellationToken) =>
        AcquireKeyLockAsync($"rider:{riderStakeholderId:D}", cancellationToken);

    public Task<int> AcquireKeyLockAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))",
            cancellationToken);

    public static bool IsFresh(
        RiderCurrentLocation? location,
        DateTimeOffset utcNow,
        RiderMatchingOptions options) =>
        location is not null && location.CapturedAtUtc >= utcNow.ToUniversalTime() - options.LocationFreshness;

    public static Point CreatePoint(decimal latitude, decimal longitude) =>
        new((double)longitude, (double)latitude) { SRID = 4326 };

    public static RiderAssignedDeliverySnapshot CreateAssignedDeliverySnapshot(Delivery delivery) =>
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
}
