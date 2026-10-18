using BLVK.Pickly.Logistics.Contracts.Payments;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Specifications;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries;

[Collection(nameof(ContainersCollection))]
public sealed class When_PersistingDeliveryAggregate_Should(ContainersFixture fixture)
    : DeliveryPersistenceIntegrationTestBase(fixture)
{
    [Fact]
    public async Task RehydrateCompleteAggregateAndAuditFields()
    {
        var trackingId = $"PKY-{Guid.NewGuid():N}";
        var (persisted, currencyId) = await PersistDeliveryAsync(trackingId);

        using var scope = CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Delivery>>();
        var rehydrated = await readRepository.FirstOrDefaultAsync(new DeliveryByTrackingIdSpecification(trackingId));

        rehydrated.ShouldNotBeNull();
        rehydrated.Id.ShouldBe(persisted.Id);
        rehydrated.Status.ShouldBe(DeliveryStatus.PendingAssignment);
        rehydrated.Items.Count.ShouldBe(2);
        rehydrated.StatusHistory.Count.ShouldBe(2);
        rehydrated.PickupAddress.AddressLine.ShouldBe("1 Persistence Road");
        rehydrated.DropOffAddress.AddressLine.ShouldBe("2 Rehydration Road");
        rehydrated.Recipient.PhoneNumber.ShouldBe("+2348012345678");
        rehydrated.Charge.Amount.ShouldBe(1234.56m);
        rehydrated.Charge.CurrencyId.ShouldBe(currencyId);
        rehydrated.ScheduledPickupAtUtc.ShouldBe(Now.AddHours(3));
        rehydrated.CreatedAtUtc.ShouldNotBe(default);
        rehydrated.Items.ShouldAllBe(item => item.CreatedAtUtc != default);
        rehydrated.StatusHistory.ShouldAllBe(history => history.CreatedAtUtc != default);
    }

    [Fact]
    public async Task EnforceUniqueTrackingIdInPostgres()
    {
        var trackingId = $"PKY-{Guid.NewGuid():N}";
        var (persisted, _) = await PersistDeliveryAsync(trackingId);

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Delivery>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repository.AddAsync(CreateDuplicate(persisted));

        await Should.ThrowAsync<DbUpdateException>(() => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task PersistMultipleAttemptsAndEnforceUniquePaymentTransactionAssociation()
    {
        var (delivery, currencyId) = await PersistDeliveryAsync($"PKY-{Guid.NewGuid():N}");
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var firstPayment = CreatePaymentTransaction(delivery.SenderId, currencyId);
        var secondPayment = CreatePaymentTransaction(delivery.SenderId, currencyId);
        dbContext.AddRange(
            firstPayment,
            secondPayment,
            DeliveryPaymentAttempt.Create(delivery.Id, firstPayment.Id, "first-attempt"),
            DeliveryPaymentAttempt.Create(delivery.Id, secondPayment.Id, "second-attempt"));
        await dbContext.SaveChangesAsync();

        var history = await dbContext.DeliveryPaymentAttempts
            .Where(attempt => attempt.DeliveryId == delivery.Id)
            .OrderBy(attempt => attempt.CreatedAtUtc)
            .ToListAsync();
        history.Count.ShouldBe(2);
        history.Select(attempt => attempt.PaymentTransactionId)
            .ShouldBe(new[] { firstPayment.Id, secondPayment.Id }, ignoreOrder: true);

        dbContext.Add(DeliveryPaymentAttempt.Create(delivery.Id, firstPayment.Id, "duplicate-association"));

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public void ConfigureExpectedSchemaIndexesAndRelationships()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deliveryType = dbContext.Model.FindEntityType(typeof(Delivery));

        deliveryType.ShouldNotBeNull();
        deliveryType.GetSchema().ShouldBe("deliveries");
        deliveryType.GetTableName().ShouldBe("Deliveries");
        deliveryType.FindProperty("ActivePaymentTransactionId").ShouldBeNull();
        deliveryType.GetIndexes().ShouldContain(index =>
            index.IsUnique && index.Properties.Single().Name == nameof(Delivery.TrackingId));
        deliveryType.GetIndexes().ShouldContain(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Delivery.SenderId), nameof(Delivery.CreatedAtUtc) }));

        var attemptType = dbContext.Model.FindEntityType(typeof(DeliveryPaymentAttempt));
        attemptType.ShouldNotBeNull();
        attemptType.GetIndexes().ShouldContain(index =>
            index.IsUnique && index.Properties.Single().Name == nameof(DeliveryPaymentAttempt.PaymentTransactionId));
    }

    private static PaymentTransaction CreatePaymentTransaction(Guid stakeholderId, Guid currencyId) =>
        PaymentTransaction.Create(
            $"delivery_{Guid.CreateVersion7():N}",
            PaymentIntent.DeliveryPayment,
            Guid.CreateVersion7(),
            1234.56m,
            currencyId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            stakeholderId,
            Guid.CreateVersion7());
}
