using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Riders;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Domain.Riders.Operations;
using BLVK.Pickly.Logistics.Domain.Riders.Persistence;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Shouldly;

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
        var matching = scope.ServiceProvider.GetRequiredService<IRiderMatchingPersistence>();
        var acceptance = scope.ServiceProvider.GetRequiredService<IRiderOfferAcceptancePersistence>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var options = MatchingOptions();
        var now = Now.AddHours(2);

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

        var processing = await matching.ProcessAsync(now, options, CancellationToken.None);
        processing.MatchingAttempts.ShouldBeGreaterThan(0);

        var offer = await dbContext.RiderDeliveryOffers.SingleOrDefaultAsync(item => item.DeliveryId == delivery.Id);
        offer.ShouldNotBeNull($"Attempts={processing.MatchingAttempts}, NoCandidates={processing.NoCandidateAttempts}");
        dbContext.ChangeTracker.Clear();
        var accepted = await acceptance.AcceptAsync(
            new(rider.StakeholderId, rider.TenantId, "test-correlation", "test-flow"),
            offer.Id,
            now.AddSeconds(1),
            options,
            CancellationToken.None);

        accepted.Code.ShouldBe(RiderOperationCode.Success);
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
        var matching = scope.ServiceProvider.GetRequiredService<IRiderMatchingPersistence>();
        var processing = await matching.ProcessAsync(Now.AddHours(2), MatchingOptions(), CancellationToken.None);

        processing.NoCandidateAttempts.ShouldBeGreaterThan(0);
        (await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderDeliveryOffers
            .AnyAsync(item => item.DeliveryId == delivery.Id)).ShouldBeFalse();
    }

    [Fact]
    public async Task EnforceSingleActiveDeliveryPerRiderInPostgres()
    {
        var (first, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var (second, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var riderId = Guid.CreateVersion7();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deliveries = await dbContext.Deliveries
            .Where(item => item.Id == first.Id || item.Id == second.Id)
            .ToListAsync();
        foreach (var delivery in deliveries)
        {
            delivery.AssignRider(riderId, Now.AddHours(2), "test-rider", "concurrency_test");
            dbContext.DeliveryStatusHistory.Add(delivery.StatusHistory.Last());
        }

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    private async Task<(Guid StakeholderId, Guid TenantId)> PersistRiderAsync(
        Delivery delivery,
        double latitude,
        double longitude)
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

        var stakeholderType = StakeholderType.Create(sender.TenantId, "Rider", "rider");
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
        var location = RiderCurrentLocation.Create(profile.Id, Now.AddHours(2), Now.AddHours(2));

        dbContext.AddRange(
            stakeholderType,
            stakeholder,
            profile,
            location,
            RiderServiceZone.Create(profile.Id, quote.OriginZoneId));
        dbContext.Entry(location).Property<Point>(RiderCurrentLocationConfiguration.PositionProperty)
            .CurrentValue = new Point(longitude, latitude) { SRID = 4326 };
        await dbContext.SaveChangesAsync();
        return (stakeholder.Id, sender.TenantId);
    }

    private static RiderMatchingOptions MatchingOptions() => new()
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
        MaximumConcurrentAssignmentsPerRider = 1
    };
}
