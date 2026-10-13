using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Application.Authentication.Features.GoogleSignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.GoogleFlows;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningInWithRegisteredGoogleIdentity_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private string _email = string.Empty;
    private string _firstName = string.Empty;
    private string _lastName = string.Empty;
    private string _idToken = string.Empty;
    private string _googleSubject = string.Empty;
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
        await CreateRegisteredGoogleUserAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnAccessToken()
    {
        GoogleSignInResponse? payload = default;

        await WhenSigningInWithGoogle();
        ThenAnAccessTokenIsReturned();

        async Task WhenSigningInWithGoogle()
        {
            using var flowResponse = await Client.PostAsync(EndpointUrl.GoogleAuthenticationFlows.V1, null);
            var flow = await flowResponse.Content.ReadFromJsonAsync<GoogleAuthenticationFlowResponse>();
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Sessions.GoogleV1,
                new GoogleSignInRequest(flow!.FlowToken, _idToken));

            payload = await _response.Content.ReadFromJsonAsync<GoogleSignInResponse>();
        }

        void ThenAnAccessTokenIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            string.IsNullOrWhiteSpace(payload?.AccessToken).ShouldBeFalse();
            new JwtSecurityTokenHandler()
                .ReadJwtToken(payload!.AccessToken).Claims
                .Any(claim => claim.Type == JwtRegisteredClaimNames.Sid)
                .ShouldBeTrue();
        }
    }

    private async Task CreateRegisteredGoogleUserAsync()
    {
        _email = WebApiIntegrationTestData.Email();
        _firstName = WebApiIntegrationTestData.FirstName();
        _lastName = WebApiIntegrationTestData.LastName();
        _googleSubject = Guid.CreateVersion7().ToString("N");
        _idToken = $"google-sign-in-{Guid.CreateVersion7():N}";
        GoogleIdentityTokenService.Register(
            _idToken,
            new GoogleIdentityTokenPayload(_googleSubject, _email, "Google User"));

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = timeProvider.GetUtcNow();
        var user = AppUser.Create(_email);
        user.MarkEmailVerified();

        var createResult = await identityService.CreateAsync(user);
        createResult.Succeeded.ShouldBeTrue();

        var addLoginResult = await identityService.AddLoginAsync(
            user,
            ExternalLoginProviders.Google,
            _googleSubject,
            "Google");
        addLoginResult.Succeeded.ShouldBeTrue();

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, _countryId, stakeholderType.Id, _firstName, _lastName);

        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _stakeholderId = stakeholder.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await repository.GetByEmailAsync(_email);

        var stakeholders = await stakeholderRepository.ListAsync(new StakeholderByIdCleanupSpecification(_stakeholderId));
        foreach (var stakeholder in stakeholders)
        {
            stakeholderRepository.Remove(stakeholder);
        }

        var stakeholderTypes = await stakeholderTypeRepository.ListAsync(new StakeholderTypeByIdCleanupSpecification(_stakeholderTypeId));
        foreach (var stakeholderType in stakeholderTypes)
        {
            stakeholderTypeRepository.Remove(stakeholderType);
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
            }
        }

        if (user is not null)
        {
            repository.Remove(user);
        }

        if (user is not null || stakeholders.Count > 0 || stakeholderTypes.Count > 0 || _createdCountryForTest)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }

    private async Task<Guid> ResolveCountryIdAsync()
    {
        using var scope = CreateScope();
        var countryReadRepository = scope.ServiceProvider.GetRequiredService<IReadRepository<Country>>();
        var countryWriteRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var countries = await countryReadRepository.ListAsync(new FirstCountrySpecification());
        if (countries.Count > 0)
        {
            return countries[0].Id;
        }

        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var country = Country.Create("Default Country", "DF", "+0", "https://example.com/flag.svg");
        await countryWriteRepository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
        _createdCountryForTest = true;

        return country.Id;
    }

    private sealed class FirstCountrySpecification : Specification<Country>
    {
        public FirstCountrySpecification()
        {
            ApplyPaging(0, 1);
        }
    }

    private sealed class StakeholderByIdCleanupSpecification : Specification<Stakeholder>
    {
        public StakeholderByIdCleanupSpecification(Guid stakeholderId)
        {
            Where(stakeholder => stakeholder.Id == stakeholderId);
        }
    }

    private sealed class StakeholderTypeByIdCleanupSpecification : Specification<StakeholderType>
    {
        public StakeholderTypeByIdCleanupSpecification(Guid stakeholderTypeId)
        {
            Where(stakeholderType => stakeholderType.Id == stakeholderTypeId);
        }
    }
}











