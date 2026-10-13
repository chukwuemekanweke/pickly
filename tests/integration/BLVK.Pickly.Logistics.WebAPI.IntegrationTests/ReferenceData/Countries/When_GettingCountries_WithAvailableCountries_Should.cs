using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetCountries;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.ReferenceData.Countries;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingCountries_WithAvailableCountries_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private Guid _countryId;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.CreateVersion7().ToString());
        await SeedCountryAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DeleteCountryAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnCountries()
    {
        IReadOnlyList<GetCountriesResponse>? payload = default;

        await WhenGettingCountries();
        ThenTheSeededCountryIsReturned();

        async Task WhenGettingCountries()
        {
            _response = await Client.GetAsync(EndpointUrl.Countries.V1);
            payload = await _response.Content.ReadFromJsonAsync<IReadOnlyList<GetCountriesResponse>>();
        }

        void ThenTheSeededCountryIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            payload.ShouldNotBeNull();
            payload.Any(country => country.CountryId == _countryId && country.ShortCode == "NGT").ShouldBeTrue();
        }
    }

    private async Task SeedCountryAsync()
    {
        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow();
        var country = Country.Create("Nigeria Test", "NGT", "+234", "https://example.com/ngt.svg");

        _countryId = country.Id;
        await repository.AddAsync(country);
        await unitOfWork.SaveChangesAsync();
    }

    private async Task DeleteCountryAsync()
    {
        if (_countryId == Guid.Empty)
        {
            return;
        }

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<Country>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var country = await repository.GetByIdAsync(_countryId);
        if (country is null)
        {
            return;
        }

        repository.Remove(country);
        await unitOfWork.SaveChangesAsync();
    }
}

