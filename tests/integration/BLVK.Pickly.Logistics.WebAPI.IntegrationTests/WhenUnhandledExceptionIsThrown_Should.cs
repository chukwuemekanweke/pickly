using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Registrations;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenUnhandledExceptionIsThrown_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string Password = "P@ssw0rd123!";

    private string _email = string.Empty;
    private Guid _tenantId;
    private Guid _countryId;
    private bool _createdCountryForTest;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        _tenantId = Guid.CreateVersion7();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        _countryId = await ResolveCountryIdAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteAuthenticationRecordsAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnFriendlyServerError()
    {
        ProblemDetails? payload = default;

        await WhenASignUpRequestTriggersAnUnhandledException();
        await ThenAFriendlyServerErrorIsReturned();

        async Task WhenASignUpRequestTriggersAnUnhandledException()
        {
            _email = WebApiIntegrationTestData.Email();

            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Registrations.V1,
                new SignUpRequest(
                    _email,
                    Password,
                    Password,
                    _countryId,
                    WebApiIntegrationTestData.FirstName(),
                    WebApiIntegrationTestData.LastName()));

            payload = await _response.Content.ReadFromJsonAsync<ProblemDetails>();
        }

        Task ThenAFriendlyServerErrorIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
            payload.ShouldNotBeNull();
            payload.Title.ShouldBe("Request failed");
            payload.Detail.ShouldBe("An unexpected error occurred while processing your request. Please try again later.");
            payload.Status.ShouldBe(StatusCodes.Status500InternalServerError);
            payload.Extensions.ContainsKey("traceId").ShouldBeTrue();

            return Task.CompletedTask;
        }
    }

    private async Task DeleteAuthenticationRecordsAsync()
    {
        using var scope = CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var countryRepository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var changesMade = false;

        if (!string.IsNullOrWhiteSpace(_email))
        {
            var user = await userRepository.GetByEmailAsync(_email);
            if (user is not null)
            {
                userRepository.Remove(user);
                changesMade = true;
            }
        }

        if (_createdCountryForTest)
        {
            var country = await countryRepository.GetByIdAsync(_countryId);
            if (country is not null)
            {
                countryRepository.Remove(country);
                changesMade = true;
            }
        }

        if (changesMade)
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
}



