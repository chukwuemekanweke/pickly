using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.UpdatePickupLocation;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.Entities;
using BLVK.Pickly.Logistics.Domain.Deliveries.Enums;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.Features.Deliveries.PickupLocations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Deliveries.PickupLocations;

[Collection(nameof(ContainersCollection))]
public sealed class When_ManagingPickupLocations_WithAuthenticatedSender_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _countryId;
    private bool _createdCountry;
    private Guid? _deliveryId;
    private Guid? _currencyId;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedSenderAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task CompletePickupLocationLifecycle()
    {
        var authorization = Client.DefaultRequestHeaders.Authorization;
        Client.DefaultRequestHeaders.Authorization = null;
        using var unauthorizedResponse = await Client.GetAsync(EndpointUrl.PickupLocations.V1);
        Client.DefaultRequestHeaders.Authorization = authorization;
        unauthorizedResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var invalidResponse = await Client.PostAsJsonAsync(
            EndpointUrl.PickupLocations.V1,
            Request("", "Invalid", latitude: 91m));
        invalidResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var firstCreateResponse = await Client.PostAsJsonAsync(
            EndpointUrl.PickupLocations.V1,
            Request("Home", "36 Garden Avenue"));
        firstCreateResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        firstCreateResponse.Headers.Location.ShouldNotBeNull();
        var first = await firstCreateResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        first.ShouldNotBeNull();
        first.IsDefault.ShouldBeTrue();

        using var secondCreateResponse = await Client.PostAsJsonAsync(
            EndpointUrl.PickupLocations.V1,
            Request("Office", "Suite C9, Elim Plaza"));
        secondCreateResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var second = await secondCreateResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        second.ShouldNotBeNull();
        second.IsDefault.ShouldBeFalse();

        using var firstPageResponse = await Client.GetAsync($"{EndpointUrl.PickupLocations.V1}?limit=1");
        var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<ListPickupLocationsResponse>();
        firstPageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstPage.ShouldNotBeNull();
        firstPage.Items.Count.ShouldBe(1);
        firstPage.HasMore.ShouldBeTrue();
        firstPage.NextCursor.ShouldNotBeNull();

        using var secondPageResponse = await Client.GetAsync(
            $"{EndpointUrl.PickupLocations.V1}?limit=1&cursor={Uri.EscapeDataString(firstPage.NextCursor)}");
        var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<ListPickupLocationsResponse>();
        secondPageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        secondPage.ShouldNotBeNull();
        secondPage.Items.Single().Id.ShouldNotBe(firstPage.Items.Single().Id);

        using var defaultResponse = await Client.GetAsync(EndpointUrl.PickupLocations.DefaultV1);
        var initialDefault = await defaultResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        defaultResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        initialDefault!.Id.ShouldBe(first.Id);

        using var byIdResponse = await Client.GetAsync(EndpointUrl.PickupLocations.ByIdV1(second.Id));
        byIdResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        using var updateResponse = await Client.PutAsJsonAsync(
            EndpointUrl.PickupLocations.ByIdV1(second.Id),
            new UpdatePickupLocationRequest("HQ", Address("24 Independence Layout")));
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var setDefaultResponse = await Client.PutAsync(
            $"{EndpointUrl.PickupLocations.ByIdV1(second.Id)}/default",
            null);
        setDefaultResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var updatedDefaultResponse = await Client.GetAsync(EndpointUrl.PickupLocations.DefaultV1);
        var updatedDefault = await updatedDefaultResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        updatedDefault!.Id.ShouldBe(second.Id);
        updatedDefault.Label.ShouldBe("HQ");

        using var deleteDefaultResponse = await Client.DeleteAsync(EndpointUrl.PickupLocations.ByIdV1(second.Id));
        deleteDefaultResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var replacementResponse = await Client.GetAsync(EndpointUrl.PickupLocations.DefaultV1);
        var replacement = await replacementResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        replacement!.Id.ShouldBe(first.Id);

        using var deleteLastResponse = await Client.DeleteAsync(EndpointUrl.PickupLocations.ByIdV1(first.Id));
        deleteLastResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var noDefaultResponse = await Client.GetAsync(EndpointUrl.PickupLocations.DefaultV1);
        noDefaultResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var deletedResponse = await Client.GetAsync(EndpointUrl.PickupLocations.ByIdV1(first.Id));
        deletedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SerializeConcurrentDefaultChanges()
    {
        var creations = await Task.WhenAll(
            CreateThroughHandlerAsync("Home", false),
            CreateThroughHandlerAsync("Office", false));

        using (var scope = CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var persisted = await dbContext.SavedPickupLocations
                .Where(location => location.SenderId == _stakeholderId)
                .ToListAsync();
            persisted.Count.ShouldBe(2);
            persisted.Count(location => location.IsDefault).ShouldBe(1);

            var entityType = dbContext.Model.FindEntityType(typeof(SavedPickupLocation));
            entityType.ShouldNotBeNull();
            entityType.GetIndexes().ShouldContain(index =>
                index.IsUnique &&
                index.GetFilter() == "\"IsDefault\" = TRUE AND \"IsDeleted\" = FALSE");
        }

        await Task.WhenAll(
            SetDefaultThroughHandlerAsync(creations[0].Location!.Id),
            SetDefaultThroughHandlerAsync(creations[1].Location!.Id));

        using var verificationScope = CreateScope();
        var verificationContext = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var defaults = await verificationContext.SavedPickupLocations
            .CountAsync(location => location.SenderId == _stakeholderId && location.IsDefault);
        defaults.ShouldBe(1);
    }

    [Fact]
    public async Task EnforceUniqueDefaultIndexInPostgres()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.SavedPickupLocations.AddRange(
            SavedPickupLocation.Create(_stakeholderId, "Home", DomainAddress("1 Road"), true),
            SavedPickupLocation.Create(_stakeholderId, "Office", DomainAddress("2 Road"), true));

        await Should.ThrowAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    [Fact]
    public async Task PreserveHistoricalDeliveryAddressSnapshotAfterSavedLocationUpdate()
    {
        var createdLocation = await CreateThroughHandlerAsync("Home", false);

        using (var scope = CreateScope())
        {
            var currencyRepository = scope.ServiceProvider.GetRequiredService<IRepository<Currency>>();
            var deliveryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Delivery>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var currency = Currency.Create($"X{Guid.NewGuid():N}"[..3].ToUpperInvariant(), "Snapshot Currency", true);
            await currencyRepository.AddAsync(currency);
            await unitOfWork.SaveChangesAsync();

            var delivery = Delivery.Create(
                _stakeholderId,
                $"PKY-{Guid.NewGuid():N}",
                DeliveryType.Instant,
                DeliveryOrderType.PayOnDelivery,
                DomainAddress("1 Original Road"),
                DomainAddress("2 Destination Road"),
                DeliveryRecipient.Create("Ada Recipient", "+2348012345678"),
                DeliveryCharge.Create(1000m, currency.Id),
                [DeliveryItem.Create("Parcel", 1, 1m)],
                DateTimeOffset.UtcNow);
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
                    "Moved Home",
                    new PickupLocationAddress("99 New Road", null, "Enugu", "Enugu", "NG", 6.45m, 7.52m),
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

    private static CreatePickupLocationRequest Request(
        string label,
        string line,
        decimal latitude = 6.4474m) =>
        new(label, Address(line, latitude));

    private static PickupLocationAddressRequest Address(
        string line,
        decimal latitude = 6.4474m) =>
        new(line, "Near the gate", "Enugu", "Enugu", "NG", latitude, 7.5139m);

    private static DeliveryAddress DomainAddress(string line) =>
        DeliveryAddress.Create(line, null, "Enugu", "Enugu", "NG", 6.4474m, 7.5139m);

    private async Task<CreatePickupLocationResult> CreateThroughHandlerAsync(string label, bool isDefault)
    {
        using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreatePickupLocationHandler>();
        return await handler.HandleAsync(
            new CreatePickupLocationCommand(
                label,
                new PickupLocationAddress("1 Road", null, "Enugu", "Enugu", "NG", 6.4474m, 7.5139m),
                isDefault,
                new ActorContext(_stakeholderId, _tenantId, "correlation", "flow")),
            CancellationToken.None);
    }

    private async Task SetDefaultThroughHandlerAsync(Guid locationId)
    {
        using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<SetDefaultPickupLocationHandler>();
        var result = await handler.HandleAsync(
            new SetDefaultPickupLocationCommand(
                locationId,
                new ActorContext(_stakeholderId, _tenantId, "correlation", "flow")),
            CancellationToken.None);
        result.Status.ShouldBe(SetDefaultPickupLocationStatus.Success);
    }

    private async Task AuthenticateAsync()
    {
        using var response = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_email, Password));
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedSenderAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var tenantRepository = scope.ServiceProvider.GetRequiredService<IRepository<Tenant>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var countryReadRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();

        var country = (await countryReadRepository.ListAsync(new FirstCountrySpecification())).FirstOrDefault();
        if (country is null)
        {
            country = Country.Create("Pickup Test Country", "PX", "+999", "https://example.com/px.svg");
            await countryRepository.AddAsync(country);
            _createdCountry = true;
        }
        var user = AppUser.Create(_email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Sender", $"sender-{Guid.NewGuid():N}");
        var tenant = Tenant.Create(_tenantId, "Pickup Location Test", $"pickup-{Guid.NewGuid():N}");
        var stakeholder = Stakeholder.Create(
            user.Id,
            _tenantId,
            country.Id,
            stakeholderType.Id,
            "Ada",
            "Lovelace");
        stakeholder.MarkVerified();

        await tenantRepository.AddAsync(tenant);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();
        _countryId = country.Id;
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        await dbContext.SavedPickupLocations
            .IgnoreQueryFilters()
            .Where(location => location.SenderId == _stakeholderId)
            .ExecuteDeleteAsync();

        if (_deliveryId.HasValue)
        {
            var delivery = await dbContext.Deliveries.FirstOrDefaultAsync(item => item.Id == _deliveryId.Value);
            if (delivery is not null)
            {
                delivery.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
                dbContext.Deliveries.Update(delivery);
            }
        }

        if (_currencyId.HasValue)
        {
            var currency = await dbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId.Value);
            if (currency is not null)
            {
                currency.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
                dbContext.Currencies.Update(currency);
            }
        }

        var stakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _stakeholderId);
        if (stakeholder is not null)
        {
            stakeholder.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
            dbContext.Stakeholders.Update(stakeholder);
        }

        var stakeholderType = await dbContext.StakeholderTypes.FirstOrDefaultAsync(item => item.Id == _stakeholderTypeId);
        if (stakeholderType is not null)
        {
            stakeholderType.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
            dbContext.StakeholderTypes.Update(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            user.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
            dbContext.Users.Update(user);
        }

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(item => item.Id == _tenantId);
        if (tenant is not null)
        {
            tenant.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
            dbContext.Tenants.Update(tenant);
        }

        if (_createdCountry)
        {
            var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == _countryId);
            if (country is not null)
            {
                country.SetDeletedAudit(DateTimeOffset.UtcNow, "test-cleanup");
                dbContext.Countries.Update(country);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
