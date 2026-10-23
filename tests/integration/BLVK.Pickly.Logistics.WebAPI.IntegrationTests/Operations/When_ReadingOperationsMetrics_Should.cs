using BLVK.Pickly.Logistics.Application.Operations.Features.GetOperationsSnapshot;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Operations.ReadModels;
using BLVK.Pickly.Logistics.Domain.Riders.Entities;
using BLVK.Pickly.Logistics.Domain.Riders.Enums;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Operations;

[Collection(nameof(ContainersCollection))]
public sealed class When_ReadingOperationsMetrics_Should(ContainersFixture fixture)
    : DeliveryPersistenceIntegrationTestBase(fixture)
{
    [Fact]
    public async Task RecordAvailabilityHistoryWhenProfileAvailabilityChanges()
    {
        var (delivery, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");

        var profileId = await PersistAvailableRiderAsync(delivery);

        using var scope = CreateScope();
        var history = await scope.ServiceProvider.GetRequiredService<AppDbContext>().RiderAvailabilityHistory
            .Where(item => item.RiderProfileId == profileId)
            .OrderBy(item => item.OccurredAtUtc).ThenBy(item => item.Id)
            .ToListAsync();
        history.Count.ShouldBe(2);
        history[0].PreviousStatus.ShouldBeNull();
        history[0].NewStatus.ShouldBe(RiderAvailabilityStatus.Offline);
        history[1].PreviousStatus.ShouldBe(RiderAvailabilityStatus.Offline);
        history[1].NewStatus.ShouldBe(RiderAvailabilityStatus.Available);
        history.ShouldAllBe(item => item.CreatedAtUtc != default);
    }

    [Fact]
    public async Task ReconstructPointInTimeStateAndDailyTransitions()
    {
        // A unique far-future day keeps these transitions apart from other tests sharing the database.
        var dayStart = new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(Random.Shared.Next(0, 3000));
        var transitionAt = dayStart.AddHours(10);
        var asOf = dayStart.AddHours(12);
        using var readerScope = CreateScope();
        var reader = readerScope.ServiceProvider.GetRequiredService<IOperationsMetricsReader>();
        var deliveriesBefore = await reader.GetDeliveryStatusCountsAsync(asOf, CancellationToken.None);
        var ridersBefore = await reader.GetRiderAvailabilityCountsAsync(asOf, CancellationToken.None);

        var (pending, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var (assigned, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var (cancelled, _) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        await PersistAvailableRiderAsync(assigned);
        await TransitionAsync(assigned.Id, delivery =>
            delivery.AssignRider(assigned.SenderId, transitionAt, "test-rider", "operations_metrics_test"));
        await TransitionAsync(cancelled.Id, delivery =>
            delivery.Cancel("No longer needed.", transitionAt, "test-sender", "operations_metrics_test"));
        // A transition after the as-of instant (on the next day) must not leak into the state or the day's counts.
        await TransitionAsync(pending.Id, delivery =>
            delivery.Cancel("Late cancellation.", dayStart.AddDays(1).AddHours(1), "test-sender", "operations_metrics_test"));

        var deliveriesAfter = await reader.GetDeliveryStatusCountsAsync(asOf, CancellationToken.None);
        var ridersAfter = await reader.GetRiderAvailabilityCountsAsync(asOf, CancellationToken.None);
        var transitions = await reader.GetDeliveryTransitionDailyCountsAsync(
            dayStart,
            dayStart.AddDays(1),
            [DeliveryStatus.RiderAssigned, DeliveryStatus.Cancelled],
            CancellationToken.None);

        Delta(deliveriesBefore, deliveriesAfter, DeliveryStatus.PendingAssignment).ShouldBe(1);
        Delta(deliveriesBefore, deliveriesAfter, DeliveryStatus.RiderAssigned).ShouldBe(1);
        Delta(deliveriesBefore, deliveriesAfter, DeliveryStatus.Cancelled).ShouldBe(1);
        BusyAvailable(ridersAfter).ShouldBe(BusyAvailable(ridersBefore) + 1);
        var day = DateOnly.FromDateTime(dayStart.UtcDateTime);
        transitions.ShouldContain(row =>
            row.Day == day && row.Status == DeliveryStatus.RiderAssigned && row.DeliveryCount == 1 && row.RiderCount == 1);
        transitions.ShouldContain(row =>
            row.Day == day && row.Status == DeliveryStatus.Cancelled && row.DeliveryCount == 1);
    }

    [Fact]
    public async Task SumCompletedDeliveryChargesByDayAndCurrency()
    {
        var dayStart = new DateTimeOffset(2035, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(Random.Shared.Next(0, 3000));
        var completedAt = dayStart.AddHours(15);
        // Each persisted delivery has its own test currency, so totals for it are isolated from other tests.
        var (completed, currencyId) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        var (assignedOnly, otherCurrencyId) = await PersistDeliveryAsync($"PKY-{Guid.CreateVersion7():N}");
        await TransitionAsync(assignedOnly.Id, delivery =>
            delivery.AssignRider(assignedOnly.SenderId, completedAt, "test-rider", "operations_metrics_test"));
        await TransitionAsync(completed.Id, delivery =>
        {
            delivery.AssignRider(completed.SenderId, completedAt.AddHours(-2), "test-rider", "operations_metrics_test");
            delivery.MarkPickedUp(completedAt.AddHours(-1), "test-rider", "operations_metrics_test");
            delivery.MarkInTransit(completedAt.AddMinutes(-30), "test-rider", "operations_metrics_test");
            delivery.ConfirmFullCashCollection(Guid.CreateVersion7(), delivery.Charge.Amount, currencyId, completedAt);
            delivery.ConfirmProofOfDelivery(Guid.CreateVersion7());
            delivery.Complete(completedAt, "test-rider", "operations_metrics_test");
        });
        using var scope = CreateScope();
        var currencyCodes = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Currencies
            .Where(currency => currency.Id == currencyId || currency.Id == otherCurrencyId)
            .ToDictionaryAsync(currency => currency.Id, currency => currency.CurrencyCode);
        var reader = scope.ServiceProvider.GetRequiredService<IOperationsMetricsReader>();

        var totals = await reader.GetCompletedDeliveryRevenueDailyTotalsAsync(
            dayStart,
            dayStart.AddDays(1),
            CancellationToken.None);

        totals.ShouldContain(new DeliveryRevenueDailyTotal(
            DateOnly.FromDateTime(dayStart.UtcDateTime),
            currencyCodes[currencyId],
            completed.Charge.Amount));
        totals.ShouldNotContain(total => total.CurrencyCode == currencyCodes[otherCurrencyId]);
    }

    [Fact]
    public async Task ServeMetricsAndDashboardAnonymously()
    {
        var snapshot = await Client.GetAsync("/api/v1/operations/metrics/snapshot");
        var timeSeries = await Client.GetAsync("/api/v1/operations/metrics/time-series?from=2026-10-01&to=2026-10-14");
        var future = await Client.GetAsync("/api/v1/operations/metrics/snapshot?date=2999-01-01");
        var dashboard = await Client.GetAsync("/operations");

        snapshot.StatusCode.ShouldBe(HttpStatusCode.OK, await snapshot.Content.ReadAsStringAsync());
        (await snapshot.Content.ReadFromJsonAsync<OperationsSnapshotResponse>()).ShouldNotBeNull();
        timeSeries.StatusCode.ShouldBe(HttpStatusCode.OK);
        future.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        dashboard.StatusCode.ShouldBe(HttpStatusCode.OK, await dashboard.Content.ReadAsStringAsync());
        dashboard.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
    }

    private async Task<Guid> PersistAvailableRiderAsync(Delivery delivery)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tenantId = await dbContext.Stakeholders
            .Where(item => item.Id == delivery.SenderId)
            .Select(item => item.TenantId)
            .SingleAsync();
        var profile = RiderProfile.Create(delivery.SenderId, tenantId, delivery.VehicleCategoryId);
        dbContext.RiderProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        profile.Approve();
        profile.SetAvailability(true);
        await dbContext.SaveChangesAsync();
        return profile.Id;
    }

    private async Task TransitionAsync(Guid deliveryId, Action<Delivery> transition)
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var delivery = await dbContext.Deliveries.SingleAsync(item => item.Id == deliveryId);
        var existingHistory = delivery.StatusHistory.Count;
        transition(delivery);
        dbContext.DeliveryStatusHistory.AddRange(delivery.StatusHistory.Skip(existingHistory));
        await dbContext.SaveChangesAsync();
    }

    private static int Delta(
        IReadOnlyList<DeliveryStatusCount> before,
        IReadOnlyList<DeliveryStatusCount> after,
        DeliveryStatus status) =>
        after.Where(row => row.Status == status).Sum(row => row.Count) -
        before.Where(row => row.Status == status).Sum(row => row.Count);

    private static int BusyAvailable(IReadOnlyList<RiderAvailabilityCount> rows) =>
        rows.Where(row => row.HasActiveDelivery && row.AvailabilityStatus == RiderAvailabilityStatus.Available)
            .Sum(row => row.Count);
}
