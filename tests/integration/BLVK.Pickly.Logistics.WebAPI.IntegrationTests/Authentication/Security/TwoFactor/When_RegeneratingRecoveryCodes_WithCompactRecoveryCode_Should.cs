using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Security;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Security.TwoFactor;

[Collection(nameof(ContainersCollection))]
public sealed class When_RegeneratingRecoveryCodes_WithCompactRecoveryCode_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private string _recoveryCode = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private bool _createdCountryForTest;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
        await CreateVerifiedUserAsync();
        await AuthenticateAsync();
        await EnableTwoFactorAndGenerateRecoveryCodeAsync();
    }

    public async Task DisposeAsync()
    {
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnReplacementCodes()
    {
        var compactRecoveryCode = _recoveryCode.Replace("-", string.Empty, StringComparison.Ordinal);

        using var response = await Client.PostAsJsonAsync(
            $"{EndpointUrl.AuthenticationSecurity.TwoFactorV1}/recovery-codes",
            new TwoFactorProofRequest("recovery_code", compactRecoveryCode));
        var payload = await response.Content.ReadFromJsonAsync<RecoveryCodesResponse>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.RecoveryCodes.Count.ShouldBe(10);
        payload.RecoveryCodes.ShouldNotContain(_recoveryCode);
    }

    [Fact]
    public async Task ReturnAStableProblemCodeForInvalidProof()
    {
        using var response = await Client.PostAsJsonAsync(
            $"{EndpointUrl.AuthenticationSecurity.TwoFactorV1}/recovery-codes",
            new TwoFactorProofRequest("recovery_code", "BCDFG-HJKLM"));
        using var payload = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        payload.RootElement.GetProperty("code").GetString().ShouldBe("invalid_two_factor_code");
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

    private async Task EnableTwoFactorAndGenerateRecoveryCodeAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var user = await identityService.FindByEmailAsync(_email);
        user.ShouldNotBeNull();
        (await identityService.SetTwoFactorEnabledAsync(user, true)).Succeeded.ShouldBeTrue();
        var recoveryCodes = await identityService.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        recoveryCodes.ShouldNotBeNull();
        _recoveryCode = recoveryCodes.First();
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
