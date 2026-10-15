using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
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

            var delivery = Delivery.Create(
                _stakeholderId,
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
}
