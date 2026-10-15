using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.Deliveries;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.CreatePickupLocation;
using BLVK.Pickly.Logistics.Application.Deliveries.Features.SetDefaultPickupLocation;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Deliveries.ValueObjects;
using BLVK.Pickly.Logistics.Domain.Locations.Entities;
using BLVK.Pickly.Logistics.Domain.Locations.ValueObjects;
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
public sealed partial class When_ManagingPickupLocations_WithAuthenticatedSender_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _countryId;
    private bool _createdCountry;
    private Guid? _deliveryId = null;
    private Guid? _currencyId = null;
    private Location _home = default!;
    private Location _office = default!;
    private Location _headquarters = default!;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedSenderAsync();
        await AuthenticateAsync();
        await SeedSharedLocationsAsync();
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
            new CreatePickupLocationRequest(Guid.Empty, ""));
        invalidResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var unknownLocationResponse = await Client.PostAsJsonAsync(
            EndpointUrl.PickupLocations.V1,
            new CreatePickupLocationRequest(Guid.CreateVersion7(), "Ghost"));
        unknownLocationResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var firstCreateResponse = await Client.PostAsJsonAsync(
            EndpointUrl.PickupLocations.V1,
            new CreatePickupLocationRequest(_home.Id, "Home", "Call when you arrive at the entrance"));
        firstCreateResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        firstCreateResponse.Headers.Location.ShouldNotBeNull();
        var first = await firstCreateResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        first.ShouldNotBeNull();
        first.IsDefault.ShouldBeTrue();
        first.LocationId.ShouldBe(_home.Id);
        first.AdditionalDirections.ShouldBe("Call when you arrive at the entrance");
        first.Address.DisplayAddress.ShouldBe(_home.Address.DisplayAddress);
        first.Address.Latitude.ShouldBe(_home.Coordinates.Latitude);
        first.Address.Longitude.ShouldBe(_home.Coordinates.Longitude);

        using var secondCreateResponse = await Client.PostAsJsonAsync(
            EndpointUrl.PickupLocations.V1,
            new CreatePickupLocationRequest(_office.Id, "Office"));
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
            new UpdatePickupLocationRequest(_headquarters.Id, "HQ", "Use the side entrance"));
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var setDefaultResponse = await Client.PutAsync(
            $"{EndpointUrl.PickupLocations.ByIdV1(second.Id)}/default",
            null);
        setDefaultResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var updatedDefaultResponse = await Client.GetAsync(EndpointUrl.PickupLocations.DefaultV1);
        var updatedDefault = await updatedDefaultResponse.Content.ReadFromJsonAsync<PickupLocationResponse>();
        updatedDefault!.Id.ShouldBe(second.Id);
        updatedDefault.Label.ShouldBe("HQ");
        updatedDefault.LocationId.ShouldBe(_headquarters.Id);
        updatedDefault.AdditionalDirections.ShouldBe("Use the side entrance");
        updatedDefault.Address.AddressLine.ShouldBe(_headquarters.Address.AddressLine);

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
        LocationGeocodingProvider.CallCount.ShouldBe(0);
    }

    private async Task SeedSharedLocationsAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        _home = SharedLocation("36 Garden Avenue", 6.4474m);
        _office = SharedLocation("Suite C9, Elim Plaza", 6.4480m);
        _headquarters = SharedLocation("24 Independence Layout", 6.4490m);
        dbContext.Locations.AddRange(_home, _office, _headquarters);
        await dbContext.SaveChangesAsync();
    }

    private static Location SharedLocation(string line, decimal latitude) =>
        Location.RegisterLandmark(
            LocationAddress.Create($"{line} {Guid.CreateVersion7():N}", "Enugu", "Enugu", "NG"),
            GeographicCoordinates.Create(latitude, 7.5139m),
            TimeProvider.System.GetUtcNow());

    private static DeliveryAddress DomainAddress(string line) =>
        DeliveryAddress.Create(line, null, "Enugu", "Enugu", "NG", 6.4474m, 7.5139m);

    private async Task<CreatePickupLocationResult> CreateThroughHandlerAsync(string label, bool isDefault)
    {
        using var scope = CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<CreatePickupLocationHandler>();
        return await handler.HandleAsync(
            new CreatePickupLocationCommand(
                _home.Id,
                label,
                null,
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

        var stakeholderType = StakeholderType.Create(_tenantId, "Sender", $"sender-{Guid.CreateVersion7():N}");
        var tenant = Tenant.Create(_tenantId, "Pickup Location Test", $"pickup-{Guid.CreateVersion7():N}");
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
        var sharedLocationIds = new[] { _home, _office, _headquarters }
            .Where(location => location is not null)
            .Select(location => location.Id)
            .ToArray();
        await dbContext.Locations
            .IgnoreQueryFilters()
            .Where(location => sharedLocationIds.Contains(location.Id))
            .ExecuteDeleteAsync();

        if (_deliveryId.HasValue)
        {
            var delivery = await dbContext.Deliveries.FirstOrDefaultAsync(item => item.Id == _deliveryId.Value);
            if (delivery is not null)
            {
                delivery.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
                dbContext.Deliveries.Update(delivery);
            }
        }

        if (_currencyId.HasValue)
        {
            var currency = await dbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId.Value);
            if (currency is not null)
            {
                currency.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
                dbContext.Currencies.Update(currency);
            }
        }

        var stakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _stakeholderId);
        if (stakeholder is not null)
        {
            stakeholder.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
            dbContext.Stakeholders.Update(stakeholder);
        }

        var stakeholderType = await dbContext.StakeholderTypes.FirstOrDefaultAsync(item => item.Id == _stakeholderTypeId);
        if (stakeholderType is not null)
        {
            stakeholderType.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
            dbContext.StakeholderTypes.Update(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            user.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
            dbContext.Users.Update(user);
        }

        var tenant = await dbContext.Tenants.FirstOrDefaultAsync(item => item.Id == _tenantId);
        if (tenant is not null)
        {
            tenant.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
            dbContext.Tenants.Update(tenant);
        }

        if (_createdCountry)
        {
            var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == _countryId);
            if (country is not null)
            {
                country.SetDeletedAudit(TimeProvider.System.GetUtcNow(), "test-cleanup");
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
