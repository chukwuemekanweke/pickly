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
using BLVK.Pickly.Logistics.WebAPI.Features.Payments.InitiatePayment;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Payments.InitiatePayment.Credo;

[Collection(nameof(ContainersCollection))]
public sealed class When_InitiatingPayment_WithCredoProvider_Should : IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private readonly WireMockServer _wireMockServer;
    private readonly CustomWebApplicationFactory _factory;

    private HttpClient _client = default!;
    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private Guid _stakeholderId;
    private Guid _stakeholderTypeId;
    private Guid _currencyId;
    private Guid _countryCurrencyId;
    private Guid _paymentProviderId;
    private Guid _paymentProviderConfigurationId;
    private Guid _paymentTransactionId;
    private HttpResponseMessage? _response;

    public When_InitiatingPayment_WithCredoProvider_Should(ContainersFixture fixture)
    {
        _wireMockServer = WireMockServer.Start();
        _factory = new CustomWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            useFakePaymentProviderServices: false,
            configurationOverrides: new Dictionary<string, string?>
            {
                ["Payments:Credo:BaseUrl"] = _wireMockServer.Urls.Single(),
                ["Payments:Credo:PublicKey"] = "0PUB_test_public_key",
                ["Payments:Credo:SecretKey"] = "test_secret_key",
                ["Payments:Credo:CallbackUrl"] = "https://backend.integration.local/payments/webhooks/credo"
            });
    }

    public async Task InitializeAsync()
    {
        ConfigureCredoStubs();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        _tenantId = Guid.CreateVersion7();
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateVerifiedUserAsync();
        await SeedPaymentSetupAsync();
        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteSeedDataAsync();
        _client.Dispose();
        await _factory.DisposeAsync();
        _wireMockServer.Dispose();
    }

    [Fact]
    public async Task ReturnPaymentLinkInstructions()
    {
        await WhenInitiatingPayment();
        await ThenThePaymentTransactionIsCreated();

        async Task WhenInitiatingPayment()
        {
            _response = await _client.PostAsJsonAsync(
                EndpointUrl.Payments.InitiateV1,
                new InitiatePaymentRequest(2500m, _currencyId, "WalletTopUp", _paymentProviderId));
        }

        async Task ThenThePaymentTransactionIsCreated()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var payload = await _response.Content.ReadFromJsonAsync<InitiatePaymentResponse>();
            payload.ShouldNotBeNull();
            payload.PaymentProviderId.ShouldBe(_paymentProviderId);
            payload.PaymentMethodType.ShouldBe(nameof(Contracts.Payments.PaymentMethodType.PaymentLink));
            payload.PaymentInstruction["paymentLink"].ShouldBe("https://checkout.credo.local/pay/auth-url");
            payload.PaymentInstruction["providerReference"].ShouldBe("cr_provider_ref_123");

            using var scope = CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
            var transaction = await dbContext.PaymentTransactions
                .OrderByDescending(item => item.CreatedAtUtc)
                .FirstAsync(item => item.PaymentProviderId == _paymentProviderId);

            _paymentTransactionId = transaction.Id;
            transaction.PaymentStatus.ShouldBe(Contracts.Payments.PaymentStatus.Initiated);
            transaction.Amount.ShouldBe(2500m);
            transaction.StakeholderId.ShouldBe(_stakeholderId);
            transaction.PaymentMethodType.ShouldBe(Contracts.Payments.PaymentMethodType.PaymentLink);
            transaction.ProviderPayloadMetadata["paymentLink"].ShouldBe("https://checkout.credo.local/pay/auth-url");
            _wireMockServer.LogEntries.Count.ShouldBeGreaterThanOrEqualTo(1);
        }
    }

    private async Task AuthenticateAsync()
    {
        var signInResponse = await _client.PostAsJsonAsync(EndpointUrl.Sessions.V1, new SignInRequest(_email, Password));
        var payload = await signInResponse.Content.ReadFromJsonAsync<SignInResponse>();

        signInResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization =
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

        var user = AppUser.Create(_email, "Ada", "Lovelace");
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

    private async Task SeedPaymentSetupAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var currency = Currency.Create("NGN", "Naira", true);
        var countryCurrency = CountryCurrency.Create(_countryId, currency.Id, true, true);
        var provider = PaymentProvider.Create("Credo", PaymentProviderKeys.Credo, true);
        provider.SetConfiguration(currency.Id, Contracts.Payments.PaymentIntent.WalletTopUp, Contracts.Payments.PaymentMethodType.PaymentLink, true);

        await dbContext.Currencies.AddAsync(currency);
        await dbContext.CountryCurrencies.AddAsync(countryCurrency);
        await dbContext.PaymentProviders.AddAsync(provider);
        await dbContext.SaveChangesAsync();

        _currencyId = currency.Id;
        _countryCurrencyId = countryCurrency.Id;
        _paymentProviderId = provider.Id;
        _paymentProviderConfigurationId = provider.Configurations.Single().Id;
    }

    private async Task DeleteSeedDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BLVK.Pickly.Logistics.Infrastructure.Persistence.AppDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();

        if (_paymentTransactionId != Guid.Empty)
        {
            var transaction = await dbContext.PaymentTransactions.FirstOrDefaultAsync(item => item.Id == _paymentTransactionId);
            if (transaction is not null)
            {
                dbContext.PaymentTransactions.Remove(transaction);
            }
        }

        var providerConfiguration = await dbContext.PaymentProviderConfigurations.FirstOrDefaultAsync(item => item.Id == _paymentProviderConfigurationId);
        if (providerConfiguration is not null)
        {
            dbContext.PaymentProviderConfigurations.Remove(providerConfiguration);
        }

        var provider = await dbContext.PaymentProviders.FirstOrDefaultAsync(item => item.Id == _paymentProviderId);
        if (provider is not null)
        {
            dbContext.PaymentProviders.Remove(provider);
        }

        var countryCurrency = await dbContext.CountryCurrencies.FirstOrDefaultAsync(item => item.Id == _countryCurrencyId);
        if (countryCurrency is not null)
        {
            dbContext.CountryCurrencies.Remove(countryCurrency);
        }

        var currency = await dbContext.Currencies.FirstOrDefaultAsync(item => item.Id == _currencyId);
        if (currency is not null)
        {
            dbContext.Currencies.Remove(currency);
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

    private IServiceScope CreateScope() => _factory.Services.CreateScope();

    private void ConfigureCredoStubs()
    {
        _wireMockServer
            .Given(Request.Create().WithPath("/transaction/initialize").UsingPost())
            .RespondWith(
                Response.Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyAsJson(new
                    {
                        status = 0,
                        message = "initialized",
                        data = new
                        {
                            authorizationUrl = "https://checkout.credo.local/pay/auth-url",
                            reference = "merchant-ref",
                            credoReference = "cr_provider_ref_123",
                            crn = "crn_123"
                        },
                        execTime = 0,
                        error = Array.Empty<string>()
                    }));
    }
}
