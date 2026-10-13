using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.Payments;
using BLVK.Pickly.Logistics.Domain.Payments.Entities;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.Providers;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Payments.Providers.SetActivation;

[Collection(nameof(ContainersCollection))]
public sealed class When_SettingPaymentProviderActivation_WithExistingProvider_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _paymentProviderId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedUserAsync();
        await SeedPaymentProviderAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ActivateProvider()
    {
        await WhenSettingActivation();
        await ThenTheProviderIsActive();

        async Task WhenSettingActivation()
        {
            _response = await Client.PutAsJsonAsync(
                $"{EndpointUrl.PaymentProviders.V1}/{_paymentProviderId}/activation",
                new SetPaymentProviderActivationRequest(true));
        }

        async Task ThenTheProviderIsActive()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
            var provider = await dbContext.PaymentProviders.FirstAsync(item => item.Id == _paymentProviderId);
            provider.IsActive.ShouldBeTrue();
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await Client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
    }

    private async Task CreateVerifiedUserAsync()
    {
        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var stakeholderTypeRepository = scope.ServiceProvider.GetRequiredService<IRepository<StakeholderType>>();
        var stakeholderRepository = scope.ServiceProvider.GetRequiredService<IRepository<Stakeholder>>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        _email = WebApiIntegrationTestData.Email();

        var user = AppUser.Create(_email);
        (await identityService.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
        user.MarkEmailVerified();
        (await identityService.UpdateAsync(user)).Succeeded.ShouldBeTrue();

        var country = Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg");
        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        var stakeholder = Stakeholder.Create(user.Id, _tenantId, country.Id, stakeholderType.Id, "Ada", "Lovelace");

        await countryRepository.AddAsync(country);
        await stakeholderTypeRepository.AddAsync(stakeholderType);
        await stakeholderRepository.AddAsync(stakeholder);
        await unitOfWork.SaveChangesAsync();

        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
        _stakeholderId = stakeholder.Id;
    }

    private async Task SeedPaymentProviderAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var provider = PaymentProvider.Create("Credo", PaymentProviderKeys.Credo, true);

        await dbContext.PaymentProviders.AddAsync(provider);
        await dbContext.SaveChangesAsync();

        _paymentProviderId = provider.Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        var provider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.Id == _paymentProviderId);
        if (provider is not null)
        {
            dbContext.PaymentProviders.Remove(provider);
        }

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

        var country = await dbContext.Countries.FirstOrDefaultAsync(item => item.Id == _countryId);
        if (country is not null)
        {
            dbContext.Countries.Remove(country);
        }

        var user = await userRepository.GetByEmailAsync(_email);
        if (user is not null)
        {
            userRepository.Remove(user);
        }

        await dbContext.SaveChangesAsync();
    }
}











