using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries.PickupLocations;

public sealed partial class When_ManagingPickupLocations_WithAuthenticatedSender_Should
{
[Fact]
    public async Task PreserveHistoricalDeliveryAddressSnapshotAfterSavedLocationUpdate()
    {
        var createdLocation = await CreateThroughHandlerAsync("Home", false);

        using (var scope = CreateScope())
        {
            var currencyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Delivery>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var currency = Currency.Create($"X{Guid.CreateVersion7():N}"[..3].ToUpperInvariant(), "Snapshot Currency", true);
            await currencyRepository.AddAsync(currency);
            await unitOfWork.SaveChangesAsync();

            var token = Guid.CreateVersion7().ToString("N")[..8];
            var vehicle = VehicleCategory.Create($"V{token}", "Snapshot van", null, 50m, "driving");
            var zone = DeliveryZone.Create($"Z{token}", "Snapshot zone", "NG", 1, TimeProvider.System.GetUtcNow().AddDays(-1));
            var policy = DeliveryPricingPolicy.Create($"p-{token}", currency.Id, 1000m, 0m, 0m, TimeProvider.System.GetUtcNow().AddDays(-1));
            var quote = DeliveryQuote.Create(
                _stakeholderId,
                Guid.CreateVersion7(),
                Guid.CreateVersion7(),
                DomainAddress("1 Original Road"),
                DomainAddress("2 Destination Road"),
                DeliveryType.Instant,
                DeliveryOrderType.PayOnDelivery,
                null,
                vehicle,
                new(zone.Id, zone.Code, zone.Id, zone.Code),
                policy,
                new(1000m, 0m, 0m, 0m, 0m),
                [("Parcel", 1, 1m)],
                TimeProvider.System.GetUtcNow(),
                TimeProvider.System.GetUtcNow().AddMinutes(15));
            var writeDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            writeDb.AddRange(vehicle, zone, policy, quote);
            writeDb.Entry(zone).Property<MultiPolygon>("Boundary").CurrentValue = Boundary();
            await writeDb.SaveChangesAsync();

            var delivery = Delivery.Create(
                _stakeholderId,
                quote.Id,
                vehicle.Id,
                $"PKY-{Guid.CreateVersion7():N}",
                DeliveryType.Instant,
                DeliveryOrderType.PayOnDelivery,
                DomainAddress("1 Original Road"),
                DomainAddress("2 Destination Road"),
                DeliveryRecipient.Create("Ada Recipient", "+2348012345678"),
                DeliveryCharge.Create(1000m, currency.Id),
                [DeliveryItem.Create("Parcel", 1, 1m)],
                TimeProvider.System.GetUtcNow());
            await deliveryRepository.AddAsync(delivery);
            await unitOfWork.SaveChangesAsync();
            _currencyId = currency.Id;
            _deliveryId = delivery.Id;
        }

        using (var scope = CreateScope())
        {
            var handler = scope.ServiceProvider.GetRequiredService<UpdatePickupLocationHandler>();
            var result = await handler.HandleAsync(
                new UpdatePickupLocationCommand(
                    createdLocation.Location!.Id,
                    _office.Id,
                    "Moved Home",
                    null,
                    new ActorContext(_stakeholderId, _tenantId, "correlation", "flow")),
                CancellationToken.None);
            result.Status.ShouldBe(UpdatePickupLocationStatus.Success);
        }

        using var verificationScope = CreateScope();
        var readRepository = verificationScope.ServiceProvider.GetRequiredService<IReadRepository<Delivery>>();
        var persisted = await readRepository.GetByIdAsync(_deliveryId.Value);
        persisted.ShouldNotBeNull();
        persisted.PickupAddress.AddressLine.ShouldBe("1 Original Road");

        var dbContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var deliveryType = dbContext.Model.FindEntityType(typeof(Delivery));
        deliveryType!.GetForeignKeys().ShouldNotContain(foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(SavedPickupLocation));
    }

    private static MultiPolygon Boundary()
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var ring = factory.CreateLinearRing(
            [new(3.30, 6.45), new(3.45, 6.45), new(3.45, 6.60), new(3.30, 6.60), new(3.30, 6.45)]);
        return factory.CreateMultiPolygon([factory.CreatePolygon(ring)]);
    }
}
