using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Providers.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.Features.Providers;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Providers;

[Collection(nameof(ContainersCollection))]
public sealed class When_ActivatingProvider_WithExistingProvider_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _activeProviderId;
    private Guid _targetProviderId;
    private string _activeProviderKey = string.Empty;
    private string _targetProviderKey = string.Empty;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateVerifiedUserAsync();
        await SeedProvidersAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task SetItActive()
    {
        await WhenActivatingProvider();
        await ThenTheProviderIsActivated();

        async Task WhenActivatingProvider()
        {
            _response = await Client.PutAsJsonAsync(
                $"{EndpointUrl.Providers.V1}/active",
                new ActivateProviderRequest("Email", _targetProviderKey));
        }

        async Task ThenTheProviderIsActivated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
            var activeProvider = await repository.GetByIdAsync(_activeProviderId);
            var targetProvider = await repository.GetByIdAsync(_targetProviderId);

            activeProvider.ShouldNotBeNull();
            targetProvider.ShouldNotBeNull();
            activeProvider.IsActive.ShouldBeFalse();
            targetProvider.IsActive.ShouldBeTrue();
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var firstName = WebApiIntegrationTestData.FirstName();
        var lastName = WebApiIntegrationTestData.LastName();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, firstName, lastName);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, _countryId, stakeholderType.Id, firstName, lastName);

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task SeedProvidersAsync()
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _activeProviderKey = $"primary-{Guid.CreateVersion7():N}";
        _targetProviderKey = $"secondary-{Guid.CreateVersion7():N}";
        var activeProvider = Provider.Create(ProviderType.Email, "Primary", _activeProviderKey, true);
        var targetProvider = Provider.Create(ProviderType.Email, "Secondary", _targetProviderKey, false);

        _activeProviderId = activeProvider.Id;
        _targetProviderId = targetProvider.Id;

        await repository.AddAsync(activeProvider);
        await repository.AddAsync(targetProvider);
        await unitOfWork.SaveChangesAsync();
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
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var providerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Provider>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        foreach (var providerId in new[] { _activeProviderId, _targetProviderId })
        {
            var provider = await providerRepository.GetByIdAsync(providerId);
            if (provider is not null)
            {
                providerRepository.Remove(provider);
            }
        }

        var stakeholder = await stakeholderRepository.GetByIdAsync(_stakeholderId);
        if (stakeholder is not null)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        var stakeholderType = await stakeholderTypeRepository.GetByIdAsync(_stakeholderTypeId);
        if (stakeholderType is not null)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        await unitOfWork.SaveChangesAsync();
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification() => ApplyPaging(0, 1);
    }
}
