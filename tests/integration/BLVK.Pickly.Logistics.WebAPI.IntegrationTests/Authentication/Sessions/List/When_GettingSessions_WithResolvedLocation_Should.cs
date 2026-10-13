using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Sessions.List;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingSessions_WithResolvedLocation_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private string _otherEmail = string.Empty;
    private string _resolvedIpAddressValue = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _otherStakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _sessionId;
    private Guid _ipAddressId;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateVerifiedUserAsync();
        await SignInAndAttachResolvedLocationAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task IncludeCityStateAndCountryForTheSession()
    {
        _response = await Client.GetAsync(EndpointUrl.Sessions.V1);
        var sessions = await _response.Content.ReadFromJsonAsync<ActiveSessionResponse[]>();

        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sessions.ShouldNotBeNull();
        sessions.Length.ShouldBe(1);
        var session = sessions.Single(item => item.SessionId == _sessionId);
        session.IsCurrent.ShouldBeTrue();
        session.LastIpAddress.ShouldBe(_resolvedIpAddressValue);
        session.City.ShouldBe("Lagos");
        session.State.ShouldBe("Lagos");
        session.Country.ShouldBe("Nigeria");
    }

    private async Task SignInAndAttachResolvedLocationAsync()
    {
        using var response = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>();
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);

        _sessionId = Guid.Parse(new JwtSecurityTokenHandler().ReadJwtToken(payload.AccessToken).Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value);

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        _resolvedIpAddressValue = $"2001:db8:2::{uniqueSuffix[..4]}:{uniqueSuffix[4..]}";
        var resolvedIpAddress = IpAddress.Create(_resolvedIpAddressValue);
        resolvedIpAddress.ApplyLocationResolution("Lagos", "Lagos", "Nigeria", DateTimeOffset.UtcNow.AddMinutes(-10));
        await dbContext.IpAddresses.AddAsync(resolvedIpAddress);
        await dbContext.SaveChangesAsync();
        _ipAddressId = resolvedIpAddress.Id;

        var session = await dbContext.AuthenticationSessions.SingleAsync(item => item.Id == _sessionId);
        session.Touch(session.LastActiveAtUtc, resolvedIpAddress.Id, session.UserAgent,
            session.DeviceName, session.DevicePlatform, session.BrowserName);
        var otherStakeholderSession = AuthenticationSession.Create(
            _otherStakeholderId,
            resolvedIpAddress.Id,
            "other-stakeholder-agent",
            "Desktop",
            "Windows",
            "Chrome",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7));
        await dbContext.AuthenticationSessions.AddAsync(otherStakeholderSession);
        await dbContext.SaveChangesAsync();
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();
        _otherEmail = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, "Ada", "Lovelace");
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        var otherUser = AppUser.Create(_otherEmail, "Grace", "Hopper");
        (await identityService.CreateAsync(otherUser, Password)).Succeeded.ShouldBeTrue();
        otherUser.MarkEmailVerified();
        (await identityService.UpdateAsync(otherUser)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(
            user.Id, _tenantId, _countryId, stakeholderType.Id, "Ada", "Lovelace");
        var otherStakeholder = Stakeholder.Create(
            otherUser.Id, _tenantId, _countryId, stakeholderType.Id, "Grace", "Hopper");
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await stakeholderRepository.AddAsync(otherStakeholder);
        await unitOfWork.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _otherStakeholderId = otherStakeholder.Id;
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

        var stakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _stakeholderId);
        if (stakeholder is not null)
        {
            dbContext.Stakeholders.Remove(stakeholder);
        }

        var otherStakeholder = await dbContext.Stakeholders.FirstOrDefaultAsync(item => item.Id == _otherStakeholderId);
        if (otherStakeholder is not null)
        {
            dbContext.Stakeholders.Remove(otherStakeholder);
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

        var otherUser = await userRepository.GetByEmailAsync(_otherEmail);
        if (otherUser is not null)
        {
            userRepository.Remove(otherUser);
        }

        await dbContext.SaveChangesAsync();

        var locations = await dbContext.IpAddressLocations
            .IgnoreQueryFilters()
            .Where(location => location.IpAddressId == _ipAddressId)
            .ToListAsync();
        dbContext.IpAddressLocations.RemoveRange(locations);

        var ipAddress = await dbContext.IpAddresses.FirstOrDefaultAsync(item => item.Id == _ipAddressId);
        if (ipAddress is not null)
        {
            dbContext.IpAddresses.Remove(ipAddress);
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
