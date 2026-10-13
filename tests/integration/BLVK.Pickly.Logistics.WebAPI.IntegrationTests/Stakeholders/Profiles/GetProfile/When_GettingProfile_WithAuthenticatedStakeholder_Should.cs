using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Application.Stakeholders.Features.GetProfile;
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
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Stakeholders.Profiles;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingProfile_WithAuthenticatedStakeholder_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";
    private const string AvatarUrl = "https://cdn.example.com/avatars/profile.jpg";

    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
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
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnProfile()
    {
        await WhenGettingProfile();
        await ThenAllProfileFieldsAreReturned();

        async Task WhenGettingProfile()
        {
            _response = await Client.GetAsync($"{EndpointUrl.Stakeholders.V1}/me/profile");
        }

        async Task ThenAllProfileFieldsAreReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var payload = await _response.Content.ReadFromJsonAsync<GetProfileResponse>();

            payload.ShouldNotBeNull();
            payload.StakeholderId.ShouldBe(_stakeholderId);
            payload.EmailAddress.ShouldBe(_email);
            payload.FirstName.ShouldBe(_firstName);
            payload.LastName.ShouldBe(_lastName);
            payload.AvatarUrl.ShouldBe(AvatarUrl);
            payload.IsVerified.ShouldBeTrue();
        }
    }

    private async Task AuthenticateAsync()
    {
        using var signInResponse = await Client.PostAsJsonAsync(
            EndpointUrl.Sessions.V1,
            new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _firstName = WebApiIntegrationTestData.FirstName();
        _lastName = WebApiIntegrationTestData.LastName();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email, _firstName, _lastName);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(
            user.Id,
            _tenantId,
            _countryId,
            stakeholderType.Id,
            _firstName,
            _lastName);
        stakeholder.SetAvatarUrl(AvatarUrl);
        stakeholder.MarkVerified();

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
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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
