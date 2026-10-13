using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.Features.Stakeholders.LoginActivity;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LoginActivityEntity = BLVK.Pickly.Logistics.Domain.Authentication.Entities.LoginActivity;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Stakeholders.LoginActivity;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingLoginActivity_WithScopedHistory_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private readonly List<Guid> _loginActivityIds = [];
    private readonly List<Guid> _ipAddressIds = [];
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _newestActivityId;
    private Guid _secondActivityId;
    private string _locatedIpAddressValue = string.Empty;
    private string _unresolvedIpAddressValue = string.Empty;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateVerifiedUserAsync();
        await AuthenticateAsync();
        await SeedLoginActivityAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnOnlyCurrentTenantStakeholderHistoryNewestFirst()
    {
        _response = await Client.GetAsync($"{EndpointUrl.Stakeholders.LoginActivityV1}?limit=2");

        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var json = await _response.Content.ReadAsStringAsync();
        var payload = await _response.Content.ReadFromJsonAsync<LoginActivityHistoryResponse>();

        payload.ShouldNotBeNull();
        payload.Activities.Select(activity => activity.Id).ShouldBe([_newestActivityId, _secondActivityId]);
        payload.Activities.ShouldAllBe(activity =>
            activity.ActivityType == nameof(LoginActivityType.InitialLogin));
        payload.Activities[0].DeviceName.ShouldBe("Desktop");
        payload.Activities[0].DevicePlatform.ShouldBe("Windows");
        payload.Activities[0].BrowserName.ShouldBe("Chrome");
        payload.Activities[0].IpAddress.ShouldBe(_locatedIpAddressValue);
        payload.Activities[0].City.ShouldBe("Lagos");
        payload.Activities[0].State.ShouldBe("Lagos");
        payload.Activities[0].Country.ShouldBe("Nigeria");
        payload.Activities[1].City.ShouldBeNull();
        payload.Activities[1].IpAddress.ShouldBe(_unresolvedIpAddressValue);
        payload.NextCursor.ShouldNotBeNull();
        json.ShouldNotContain("userAgent", Case.Insensitive);
        json.ShouldNotContain("198.51.100", Case.Insensitive);
    }

    private async Task SeedLoginActivityAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        _locatedIpAddressValue = $"2001:db8::{uniqueSuffix[..4]}:{uniqueSuffix[4..]}";
        _unresolvedIpAddressValue = $"2001:db8:1::{uniqueSuffix[..4]}:{uniqueSuffix[4..]}";
        var locatedIpAddress = IpAddress.Create(_locatedIpAddressValue);
        locatedIpAddress.ApplyLocationResolution("Lagos", "Lagos", "Nigeria", DateTimeOffset.UtcNow.AddMinutes(-10));
        var unresolvedIpAddress = IpAddress.Create(_unresolvedIpAddressValue);

        await dbContext.IpAddresses.AddRangeAsync(locatedIpAddress, unresolvedIpAddress);
        await dbContext.SaveChangesAsync();
        _ipAddressIds.Add(locatedIpAddress.Id);
        _ipAddressIds.Add(unresolvedIpAddress.Id);

        var locationId = locatedIpAddress.GetCurrentLocation()!.Id;
        var baseline = DateTimeOffset.UtcNow.AddMinutes(-5);
        var newest = LoginActivityEntity.CreateInitialLogin(
            _stakeholderId, _tenantId, locatedIpAddress.Id, locationId,
            "integration-test-agent-sensitive", "Desktop", "Windows", "Chrome", baseline.AddMinutes(3));
        var refresh = LoginActivityEntity.CreateTokenRefresh(
            _stakeholderId, _tenantId, locatedIpAddress.Id, locationId,
            "integration-test-agent-sensitive", "Desktop", "Windows", "Chrome", baseline.AddMinutes(2.5));
        var second = LoginActivityEntity.CreateInitialLogin(
            _stakeholderId, _tenantId, unresolvedIpAddress.Id, null,
            "integration-test-agent-sensitive", "Phone", "Android", "Chrome", baseline.AddMinutes(2));
        var third = LoginActivityEntity.CreateInitialLogin(
            _stakeholderId, _tenantId, locatedIpAddress.Id, locationId,
            "integration-test-agent-sensitive", "Desktop", "Windows", "Chrome", baseline.AddMinutes(1));
        var otherStakeholder = LoginActivityEntity.CreateInitialLogin(
            Guid.CreateVersion7(), _tenantId, locatedIpAddress.Id, locationId,
            "must-not-leak", null, null, null, baseline.AddMinutes(5));
        var otherTenant = LoginActivityEntity.CreateInitialLogin(
            _stakeholderId, Guid.CreateVersion7(), locatedIpAddress.Id, locationId,
            "must-not-leak", null, null, null, baseline.AddMinutes(4));

        await dbContext.LoginActivities.AddRangeAsync(
            newest, refresh, second, third, otherStakeholder, otherTenant);
        await dbContext.SaveChangesAsync();

        _newestActivityId = newest.Id;
        _secondActivityId = second.Id;
        _loginActivityIds.AddRange(
            [newest.Id, refresh.Id, second.Id, third.Id, otherStakeholder.Id, otherTenant.Id]);
    }

    private async Task AuthenticateAsync()
    {
        using var response = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(
            user.Id, _tenantId, _countryId, stakeholderType.Id, "Ada", "Lovelace");
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var readRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var writeRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var existing = await readRepository.ListAsync(new FirstCountrySpecification());
        if (existing.Count > 0)
        {
            return existing[0].Id;
        }

        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await writeRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;
        return country.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        var activities = await dbContext.LoginActivities
            .IgnoreQueryFilters()
            .Where(activity => _loginActivityIds.Contains(activity.Id))
            .ToListAsync();
        dbContext.LoginActivities.RemoveRange(activities);

        var locations = await dbContext.IpAddressLocations
            .IgnoreQueryFilters()
            .Where(location => _ipAddressIds.Contains(location.IpAddressId))
            .ToListAsync();
        dbContext.IpAddressLocations.RemoveRange(locations);

        var ipAddresses = await dbContext.IpAddresses
            .IgnoreQueryFilters()
            .Where(ipAddress => _ipAddressIds.Contains(ipAddress.Id))
            .ToListAsync();
        dbContext.IpAddresses.RemoveRange(ipAddresses);

        var stakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _stakeholderId);
        if (stakeholder is not null)
        {
            dbContext.Stakeholders.Remove(stakeholder);
        }

        var stakeholderType = await dbContext.StakeholderTypes.FirstOrDefaultAsync(item => item.Id == _stakeholderTypeId);
        if (stakeholderType is not null)
        {
            dbContext.StakeholderTypes.Remove(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        if (_createdCountryForTest)
        {
            var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == _countryId);
            if (country is not null)
            {
                dbContext.Countries.Remove(country);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
