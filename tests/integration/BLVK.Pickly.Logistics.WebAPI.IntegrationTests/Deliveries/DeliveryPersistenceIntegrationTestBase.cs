using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Persistence.Configurations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
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
        var currency = Currency.Create(
            await UnusedCurrencyCodeAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>()),
            $"Test Currency {suffix}",
            true);
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

        var vehicle = VehicleCategory.Create($"V{suffix}", "Persistence van", null, 100m, "driving");
        var zone = DeliveryZone.Create($"Z{suffix}", "Persistence zone", "NG", 1, Now.AddDays(-1));
        var policy = DeliveryPricingPolicy.Create($"p-{suffix}", currency.Id, 1234.56m, 0m, 0m, Now.AddDays(-1));
        var quote = DeliveryQuote.Create(
            sender.Id,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Address("1 Persistence Road"),
            Address("2 Rehydration Road"),
            DeliveryType.Scheduled,
            DeliveryOrderType.PayOnDelivery,
            Now.AddHours(3),
            vehicle,
            new(zone.Id, zone.Code, zone.Id, zone.Code),
            policy,
            new(1234.56m, 0m, 0m, 0m, 0m),
            [("Documents", 1, 0.250m), ("Shoes", 2, 1.750m)],
            Now.AddMinutes(-1),
            Now.AddHours(1));
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.AddRange(vehicle, zone, policy, quote);
        dbContext.Entry(zone).Property<MultiPolygon>(DeliveryZoneConfiguration.BoundaryProperty)
            .CurrentValue = Boundary();
        await dbContext.SaveChangesAsync();

        var delivery = Delivery.Create(
            sender.Id,
            quote.Id,
            vehicle.Id,
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
            source.DeliveryQuoteId,
            source.VehicleCategoryId,
            source.TrackingId,
            DeliveryType.Instant,
            DeliveryOrderType.PayOnDelivery,
            Address("3 Duplicate Road"),
            Address("4 Duplicate Road"),
            DeliveryRecipient.Create("Second Recipient", "+2348098765432"),
            DeliveryCharge.Create(source.Charge.Amount, source.Charge.CurrencyId),
            [DeliveryItem.Create("Duplicate", 1, 1m)],
            Now);

    // Currency codes are three characters wide; random codes collide once a test run creates dozens of deliveries.
    private static async Task<string> UnusedCurrencyCodeAsync(AppDbContext dbContext)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        while (true)
        {
            var code = string.Concat(Enumerable.Range(0, 3).Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]));
            if (!await dbContext.Set<Currency>().IgnoreQueryFilters().AnyAsync(currency => currency.CurrencyCode == code))
            {
                return code;
            }
        }
    }

    private static DeliveryAddress Address(string line) =>
        DeliveryAddress.Create(line, "Gate 1", "Lagos", "Lagos", "NG", 6.5244m, 3.3792m);

    private static MultiPolygon Boundary()
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var ring = factory.CreateLinearRing(
            [new(3.30, 6.45), new(3.45, 6.45), new(3.45, 6.60), new(3.30, 6.60), new(3.30, 6.45)]);
        return factory.CreateMultiPolygon([factory.CreatePolygon(ring)]);
    }
}
