using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Application.Deliveries.Quotes;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Bookings;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.Quotes;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DomainLocation = BLVK.Pickly.Logistics.Domain.Locations.Entities.Location;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries.Quotes;

[Collection(nameof(ContainersCollection))]
public sealed class When_CreatingAndRetrievingDeliveryQuote_Should(ContainersFixture fixture) : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private SeededSender _sender = default!;
    private readonly Currency _currency = Currency.Create("TST", "Test currency", true);
    private readonly VehicleCategory _vehicle = VehicleCategory.Create($"V{Guid.CreateVersion7():N}"[..20], "Test van", null, 100m, "driving");
    private readonly DeliveryZone _zone = DeliveryZone.Create($"Z{Guid.CreateVersion7():N}"[..20], "Test zone", "NG", 1, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private DomainLocation _pickup = default!;
    private DomainLocation _dropOff = default!;
    private SavedPickupLocation _savedPickup = default!;
    private DeliveryPricingPolicy _policy = default!;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        using (var scope = CreateScope()) _sender = await IntegrationSenderSeeder.CreateVerifiedSenderAsync(scope.ServiceProvider, Guid.CreateVersion7());
        _pickup = DomainLocation.RegisterLandmark(LocationAddress.Create("1 Quote Street", "Enugu", "Enugu", "NG"), GeographicCoordinates.FromDegrees(6.44, 7.51), new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
        _dropOff = DomainLocation.RegisterLandmark(LocationAddress.Create("2 Quote Avenue", "Enugu", "Enugu", "NG"), GeographicCoordinates.FromDegrees(6.46, 7.53), new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));
        _savedPickup = SavedPickupLocation.Create(_sender.StakeholderId, "Warehouse", _pickup.Id, "Blue gate", true);
        _policy = DeliveryPricingPolicy.Create($"test-{Guid.CreateVersion7():N}", _currency.Id, 100m, 10m, 5m, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _policy.AddVehicleRule(_vehicle.Id, 20m);
        _policy.AddZoneRule(_zone.Id, _zone.Id, 30m);
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(_currency, _vehicle, _zone, _pickup, _dropOff, _savedPickup, _policy);
            db.Entry(_zone).Property<MultiPolygon>("Boundary").CurrentValue = Boundary();
            await db.SaveChangesAsync();
        }
        DeliveryRouteProvider.Responder = _ => new(DeliveryRouteStatus.Success, 8000, TimeSpan.FromMinutes(20), "fake", "driving");
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _sender.TenantId.ToString());
        using var signIn = await Client.PostAsJsonAsync("/api/v1/sessions", new SignInRequest(_sender.Email, IntegrationSenderSeeder.Password));
        var session = await signIn.Content.ReadFromJsonAsync<SignInResponse>();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
    }

    public async Task DisposeAsync()
    {
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var deliveryIds = await db.Deliveries.IgnoreQueryFilters().Where(x => x.SenderId == _sender.StakeholderId).Select(x => x.Id).ToListAsync();
            await db.DeliveryBookingConfirmations.IgnoreQueryFilters().Where(x => deliveryIds.Contains(x.DeliveryId)).ExecuteDeleteAsync();
            await db.DeliveryQuotes.IgnoreQueryFilters().Where(x => x.SenderId == _sender.StakeholderId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ConsumptionStatus, DeliveryQuoteConsumptionStatus.Active)
                    .SetProperty(x => x.ConsumedByDeliveryId, (Guid?)null)
                    .SetProperty(x => x.ConsumedAtUtc, (DateTimeOffset?)null));
            await db.DeliveryStatusHistory.IgnoreQueryFilters().Where(x => deliveryIds.Contains(x.DeliveryId)).ExecuteDeleteAsync();
            await db.DeliveryItems.IgnoreQueryFilters().Where(x => deliveryIds.Contains(x.DeliveryId)).ExecuteDeleteAsync();
            await db.Deliveries.IgnoreQueryFilters().Where(x => deliveryIds.Contains(x.Id)).ExecuteDeleteAsync();
            var quoteIds = await db.DeliveryQuotes.IgnoreQueryFilters().Where(x => x.SenderId == _sender.StakeholderId).Select(x => x.Id).ToListAsync();
            await db.DeliveryQuoteItems.IgnoreQueryFilters().Where(x => quoteIds.Contains(x.DeliveryQuoteId)).ExecuteDeleteAsync();
            await db.DeliveryQuotes.IgnoreQueryFilters().Where(x => quoteIds.Contains(x.Id)).ExecuteDeleteAsync();
            await db.DeliveryVehiclePricingRules.IgnoreQueryFilters().Where(x => x.DeliveryPricingPolicyId == _policy.Id).ExecuteDeleteAsync();
            await db.DeliveryZonePricingRules.IgnoreQueryFilters().Where(x => x.DeliveryPricingPolicyId == _policy.Id).ExecuteDeleteAsync();
            await db.DeliveryPricingPolicies.IgnoreQueryFilters().Where(x => x.Id == _policy.Id).ExecuteDeleteAsync();
            await db.SavedPickupLocations.IgnoreQueryFilters().Where(x => x.Id == _savedPickup.Id).ExecuteDeleteAsync();
            await db.Locations.IgnoreQueryFilters().Where(x => x.Id == _pickup.Id || x.Id == _dropOff.Id).ExecuteDeleteAsync();
            await db.DeliveryZones.IgnoreQueryFilters().Where(x => x.Id == _zone.Id).ExecuteDeleteAsync();
            await db.VehicleCategories.IgnoreQueryFilters().Where(x => x.Id == _vehicle.Id).ExecuteDeleteAsync();
            await db.Currencies.IgnoreQueryFilters().Where(x => x.Id == _currency.Id).ExecuteDeleteAsync();
        }
        using (var scope = CreateScope()) await IntegrationSenderSeeder.DeleteAsync(scope.ServiceProvider, _sender);
        await DisposeClientAsync();
    }

    [Fact]
    public async Task PersistAuthoritativeSnapshot_AndRetrieveWithoutRerouting()
    {
        var request = new CreateDeliveryQuoteRequest(_savedPickup.Id, _dropOff.Id, "Instant", "PayOnDelivery", _vehicle.Id, [new("Parcel", 2, 3m)]);
        using var create = await Client.PostAsJsonAsync("/api/v1/delivery-quotes", request);
        var created = await create.Content.ReadFromJsonAsync<DeliveryQuoteResponse>();
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.ShouldNotBeNull();
        created.Pricing.TotalAmount.ShouldBe(260m);
        created.PaymentResponsibleParty.ShouldBe("Recipient");
        created.PaymentMethod.ShouldBe("Cash");
        created.Pickup.AdditionalInformation.ShouldBe("Blue gate");
        DeliveryRouteProvider.RequestCount.ShouldBe(1);

        using var get = await Client.GetAsync($"/api/v1/delivery-quotes/{created.QuoteId}");
        var retrieved = await get.Content.ReadFromJsonAsync<DeliveryQuoteResponse>();
        get.StatusCode.ShouldBe(HttpStatusCode.OK);
        retrieved.ShouldNotBeNull();
        retrieved.QuoteId.ShouldBe(created.QuoteId);
        retrieved.Pricing.ShouldBe(created.Pricing);
        retrieved.Items.Count.ShouldBe(created.Items.Count);
        retrieved.Items[0].ShouldBe(created.Items[0]);
        DeliveryRouteProvider.RequestCount.ShouldBe(1);
    }

    [Fact]
    public async Task ConfirmBookingAtomically_AndReturnItForMatchingRetry()
    {
        var quoteRequest = new CreateDeliveryQuoteRequest(
            _savedPickup.Id,
            _dropOff.Id,
            "Instant",
            "PayOnDelivery",
            _vehicle.Id,
            [new("Parcel", 2, 3m)]);
        using var quoteResponse = await Client.PostAsJsonAsync("/api/v1/delivery-quotes", quoteRequest);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<DeliveryQuoteResponse>();
        quoteResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var bookingRequest = new CreateDeliveryRequest(
            quote!.QuoteId,
            new("Ada Recipient", "+2348012345678"),
            "No. 23, blue gate");
        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deliveries")
        {
            Content = JsonContent.Create(bookingRequest)
        };
        createRequest.Headers.Add("Idempotency-Key", "phase-4-integration-booking");
        using var createResponse = await Client.SendAsync(createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<DeliveryResponse>();

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        created.ShouldNotBeNull();
        created.QuoteId.ShouldBe(quote.QuoteId);
        created.VehicleCategoryId.ShouldBe(_vehicle.Id);
        created.Status.ShouldBe("PendingAssignment");
        created.PaymentStatus.ShouldBe("Pending");
        created.Charge.Amount.ShouldBe(260m);
        created.Items.Single().Quantity.ShouldBe(2);
        created.PickupAddress.AdditionalInformation.ShouldBe("Blue gate");
        created.DropOffAddress.AdditionalInformation.ShouldBe("No. 23, blue gate");
        created.TrackingId.ShouldStartWith("PKL-");

        using var retryRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deliveries")
        {
            Content = JsonContent.Create(bookingRequest)
        };
        retryRequest.Headers.Add("Idempotency-Key", "phase-4-integration-booking");
        using var retryResponse = await Client.SendAsync(retryRequest);
        var retried = await retryResponse.Content.ReadFromJsonAsync<DeliveryResponse>();
        retryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        retried!.DeliveryId.ShouldBe(created.DeliveryId);

        var conflictingRequest = bookingRequest with
        {
            Recipient = new("Different Recipient", "+2348012345678")
        };
        using var conflictMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deliveries")
        {
            Content = JsonContent.Create(conflictingRequest)
        };
        conflictMessage.Headers.Add("Idempotency-Key", "phase-4-integration-booking");
        using var conflictResponse = await Client.SendAsync(conflictMessage);
        conflictResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        using var getResponse = await Client.GetAsync($"/api/v1/deliveries/{created.DeliveryId}");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<DeliveryResponse>();
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        retrieved!.DeliveryId.ShouldBe(created.DeliveryId);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var persistedQuote = await db.DeliveryQuotes.SingleAsync(item => item.Id == quote.QuoteId);
        persistedQuote.ConsumedByDeliveryId.ShouldBe(created.DeliveryId);
        (await db.Deliveries.CountAsync(item => item.DeliveryQuoteId == quote.QuoteId)).ShouldBe(1);
        (await db.DeliveryBookingConfirmations.CountAsync(item => item.DeliveryId == created.DeliveryId)).ShouldBe(1);
    }

    [Fact]
    public async Task AllowOnlyOneConcurrentBookingForAQuote()
    {
        var quoteRequest = new CreateDeliveryQuoteRequest(
            _savedPickup.Id,
            _dropOff.Id,
            "Instant",
            "PayOnDelivery",
            _vehicle.Id,
            [new("Concurrent parcel", 1, 2m)]);
        using var quoteResponse = await Client.PostAsJsonAsync("/api/v1/delivery-quotes", quoteRequest);
        var quote = await quoteResponse.Content.ReadFromJsonAsync<DeliveryQuoteResponse>();
        var booking = new CreateDeliveryRequest(
            quote!.QuoteId,
            new("Concurrent Recipient", "+2348012345678"),
            "Gate 2");

        using var firstRequest = BookingMessage(booking, "concurrent-key-a");
        using var secondRequest = BookingMessage(booking, "concurrent-key-b");
        var responses = await Task.WhenAll(Client.SendAsync(firstRequest), Client.SendAsync(secondRequest));
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];
        var statuses = responses.Select(response => response.StatusCode).Order().ToArray();
        statuses.ShouldBe([HttpStatusCode.OK, HttpStatusCode.Created], ignoreOrder: true);
        var first = await firstResponse.Content.ReadFromJsonAsync<DeliveryResponse>();
        var second = await secondResponse.Content.ReadFromJsonAsync<DeliveryResponse>();
        first!.DeliveryId.ShouldBe(second!.DeliveryId);

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Deliveries.CountAsync(item => item.DeliveryQuoteId == quote.QuoteId)).ShouldBe(1);
    }

    private static HttpRequestMessage BookingMessage(CreateDeliveryRequest request, string key)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/v1/deliveries")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("Idempotency-Key", key);
        return message;
    }

    private static MultiPolygon Boundary()
    {
        var factory = new GeometryFactory(new PrecisionModel(), 4326);
        var ring = factory.CreateLinearRing([new(7.40, 6.35), new(7.60, 6.35), new(7.60, 6.55), new(7.40, 6.55), new(7.40, 6.35)]);
        return factory.CreateMultiPolygon([factory.CreatePolygon(ring)]);
    }
}
