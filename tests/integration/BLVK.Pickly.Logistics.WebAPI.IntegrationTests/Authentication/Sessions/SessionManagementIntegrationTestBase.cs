using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Sessions;

public abstract class SessionManagementIntegrationTestBase(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private readonly List<HttpResponseMessage> _responses = [];
    private string _email = string.Empty;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private bool _createdCountry;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        var tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        _email = WebApiIntegrationTestData.Email();

        using var scope = CreateScope();
        var countries = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var stakeholderTypes = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholders = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var identity = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var country = (await countries.ListAsync(new FirstCountrySpecification())).FirstOrDefault();
        if (country is null)
        {
            country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
            await countryRepository.AddAsync(country);
            _createdCountry = true;
        }
        _countryId = country.Id;

        var user = AppUser.Create(_email);
        (await identity.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identity.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var type = StakeholderType.Create(tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, tenantId, country.Id, type.Id, "Jane", "Doe");
        await stakeholderTypes.AddAsync(type);
        await stakeholders.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();
        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = type.Id;
    }

    public async Task DisposeAsync()
    {
        foreach (var response in _responses) response.Dispose();
        if (!string.IsNullOrEmpty(_email))
        {
            using var scope = CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
            var stakeholders = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
            var types = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
            var countries = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var stakeholder = await stakeholders.GetByIdAsync(_stakeholderId);
            if (stakeholder is not null) stakeholders.Remove(stakeholder);
            var type = await types.GetByIdAsync(_stakeholderTypeId);
            if (type is not null) types.Remove(type);
            if (_createdCountry)
            {
                var country = await countries.GetByIdAsync(_countryId);
                if (country is not null) countries.Remove(country);
            }
            var user = await users.GetByEmailAsync(_email);
            if (user is not null) users.Remove(user);
            await unitOfWork.SaveChangesAsync();
        }
        await DisposeClientAsync();
    }

    protected async Task<SignInResponse> SignInAsync()
    {
        var response = Track(await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1,
            new SignInRequest(_email, Password)));
        response.IsSuccessStatusCode.ShouldBeTrue(await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<SignInResponse>())!;
    }

    protected void UseAccessToken(string accessToken) =>
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    protected HttpResponseMessage Track(HttpResponseMessage response)
    {
        _responses.Add(response);
        return response;
    }

    protected HttpClient HttpClient => Client;

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
