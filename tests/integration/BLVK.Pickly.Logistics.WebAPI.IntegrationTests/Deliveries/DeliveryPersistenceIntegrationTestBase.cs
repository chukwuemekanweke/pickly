using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries;

public abstract class DeliveryPersistenceIntegrationTestBase(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    protected static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

    public Task InitializeAsync() => InitializeClientAsync();

    public Task DisposeAsync() => DisposeClientAsync();

    protected async Task<(Delivery Delivery, Guid CurrencyId)> PersistDeliveryAsync(string trackingId)
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var tenantRepository = scope.ServiceProvider.GetRequiredService<IRepository<Tenant>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var currencyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
        var deliveryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Delivery>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenant = Tenant.Create(Guid.CreateVersion7(), $"Delivery Test {suffix}", $"delivery-{suffix}");
        var stakeholderType = StakeholderType.Create(tenant.Id, "Sender", $"sender-{suffix}");
        var currency = Currency.Create($"X{suffix[..2]}".ToUpperInvariant(), $"Test Currency {suffix}", true);
        var user = AppUser.Create($"delivery-{suffix}@example.com");
        (await identityService.CreateAsync(user, "P@ssw0rd123!")).Succeeded.ShouldBeTrue();
        var sender = Stakeholder.Create(
            user.Id,
            tenant.Id,
            Guid.CreateVersion7(),
            stakeholderType.Id,
            "Test",
            "Sender");

        await tenantRepository.AddAsync(tenant);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await currencyRepository.AddAsync(currency);
        await unitOfWork.SaveChangesAsync();
        await stakeholderRepository.AddAsync(sender);
        await unitOfWork.SaveChangesAsync();

        var delivery = Delivery.Create(
            sender.Id,
            trackingId,
            DeliveryType.Scheduled,
            DeliveryOrderType.PayOnDelivery,
            Address("1 Persistence Road"),
            Address("2 Rehydration Road"),
            DeliveryRecipient.Create("Ada Recipient", "+2348012345678"),
            DeliveryCharge.Create(1234.56m, currency.Id),
            [
                DeliveryItem.Create("Documents", 1, 0.250m),
                DeliveryItem.Create("Shoes", 2, 1.750m)
            ],
            Now,
            Now.AddHours(3),
            Now.AddHours(2));

        delivery.OpenDispatch(Now.AddHours(2), "scheduler", "scheduled_dispatch");
        await deliveryRepository.AddAsync(delivery);
        await unitOfWork.SaveChangesAsync();

        return (delivery, currency.Id);
    }

    protected static Delivery CreateDuplicate(Delivery source) =>
        Delivery.Create(
            source.SenderId,
            source.TrackingId,
            DeliveryType.Instant,
            DeliveryOrderType.PayOnDelivery,
            Address("3 Duplicate Road"),
            Address("4 Duplicate Road"),
            DeliveryRecipient.Create("Second Recipient", "+2348098765432"),
            DeliveryCharge.Create(source.Charge.Amount, source.Charge.CurrencyId),
            [DeliveryItem.Create("Duplicate", 1, 1m)],
            Now);

    private static DeliveryAddress Address(string line) =>
        DeliveryAddress.Create(line, "Gate 1", "Lagos", "Lagos", "NG", 6.5244m, 3.3792m);
}
