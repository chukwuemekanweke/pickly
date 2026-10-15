using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BLVK.Pickly.Logistics.Infrastructure.UnitTests.Persistence;

public sealed class When_ConfiguringDeliveryPersistence_Should
{
    [Fact]
    public void ExposeExpectedSchemaIndexesOwnedValuesAndRelationships()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=model_test;Username=postgres;Password=postgres")
            .Options;
        using var dbContext = new AppDbContext(options);
        var deliveryType = dbContext.Model.FindEntityType(typeof(Delivery));

        deliveryType.ShouldNotBeNull();
        deliveryType.GetSchema().ShouldBe("deliveries");
        deliveryType.GetTableName().ShouldBe("Deliveries");
        deliveryType.GetIndexes().ShouldContain(index =>
            index.IsUnique && index.Properties.Single().Name == nameof(Delivery.TrackingId));
        deliveryType.GetIndexes().ShouldContain(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(Delivery.TenantId), nameof(Delivery.SenderId), nameof(Delivery.CreatedAtUtc) }));
        deliveryType.FindNavigation(nameof(Delivery.Items)).ShouldNotBeNull();
        deliveryType.FindNavigation(nameof(Delivery.StatusHistory)).ShouldNotBeNull();
        deliveryType.FindNavigation(nameof(Delivery.PickupAddress)).ShouldNotBeNull();
        deliveryType.FindNavigation(nameof(Delivery.DropOffAddress)).ShouldNotBeNull();
        deliveryType.FindNavigation(nameof(Delivery.Recipient)).ShouldNotBeNull();
        deliveryType.FindNavigation(nameof(Delivery.Charge)).ShouldNotBeNull();
    }
}
