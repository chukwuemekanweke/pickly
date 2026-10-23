using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.ReferenceData.Entities;
using BLVK.Pickly.Logistics.Domain.Stakeholders.Entities;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Sessions.DeviceSignIn;

[Collection(nameof(ContainersCollection))]
public sealed class WhenSigningInWithDeviceIdentifier_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private readonly string _deviceIdentifier = $"integration-device-{Guid.CreateVersion7():N}";
    private readonly Guid _tenantId = Guid.CreateVersion7();
    private Guid _countryId;
    private Guid _stakeholderTypeId;
    private bool _createdCountry;
    private HttpResponseMessage? _firstResponse;
    private HttpResponseMessage? _secondResponse;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
        await CreateReferenceDataAsync();
    }

    public async Task DisposeAsync()
    {
        _firstResponse?.Dispose();
        _secondResponse?.Dispose();
        await DeleteCreatedAccountAsync();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task CreateOneAccountAndReturnTokensForEveryRequest()
    {
        var request = new DeviceSignInRequest(_deviceIdentifier);

        _firstResponse = await Client.PostAsJsonAsync(EndpointUrl.Sessions.DeviceV1, request);
        _secondResponse = await Client.PostAsJsonAsync(EndpointUrl.Sessions.DeviceV1, request);

        var firstPayload = await _firstResponse.Content.ReadFromJsonAsync<SignInResponse>();
        var secondPayload = await _secondResponse.Content.ReadFromJsonAsync<SignInResponse>();

        _firstResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await _firstResponse.Content.ReadAsStringAsync());
        _secondResponse.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await _secondResponse.Content.ReadAsStringAsync());
        firstPayload.ShouldNotBeNull();
        secondPayload.ShouldNotBeNull();
        firstPayload.Outcome.ShouldBe("authenticated");
        secondPayload.Outcome.ShouldBe("authenticated");
        firstPayload.AccessToken.ShouldNotBeNullOrWhiteSpace();
        secondPayload.AccessToken.ShouldNotBeNullOrWhiteSpace();
        firstPayload.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        secondPayload.RefreshToken.ShouldNotBeNullOrWhiteSpace();

        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mappings = await dbContext.DeviceAccounts
            .Where(account => account.Identifier == _deviceIdentifier)
            .ToListAsync();

        mappings.Count.ShouldBe(1);
        (await dbContext.Users.CountAsync(user => user.Id == mappings[0].AppUserId)).ShouldBe(1);
        (await dbContext.Stakeholders.CountAsync(stakeholder => stakeholder.Id == mappings[0].StakeholderId)).ShouldBe(1);
    }

    private async Task DeleteCreatedAccountAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var mapping = await dbContext.DeviceAccounts
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(account => account.Identifier == _deviceIdentifier);
        if (mapping is not null)
        {
            var stakeholder = await dbContext.Stakeholders
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == mapping.StakeholderId);
            var user = await dbContext.Users
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == mapping.AppUserId);

            dbContext.DeviceAccounts.Remove(mapping);
            dbContext.Stakeholders.Remove(stakeholder);
            dbContext.Users.Remove(user);
        }

        var stakeholderType = await dbContext.StakeholderTypes
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(item => item.Id == _stakeholderTypeId);
        if (stakeholderType is not null)
        {
            dbContext.StakeholderTypes.Remove(stakeholderType);
        }

        if (_createdCountry)
        {
            var country = await dbContext.Countries
                .IgnoreQueryFilters()
                .SingleAsync(item => item.Id == _countryId);
            dbContext.Countries.Remove(country);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task CreateReferenceDataAsync()
    {
        using var scope = CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var country = await dbContext.Countries.SingleOrDefaultAsync(item => item.ShortCode == "NG");
        if (country is null)
        {
            country = Country.Create("Nigeria", "NG", "+234", "https://example.com/ng.svg");
            await dbContext.Countries.AddAsync(country);
            _createdCountry = true;
        }

        var stakeholderType = StakeholderType.Create(_tenantId, "Customer", "customer");
        await dbContext.StakeholderTypes.AddAsync(stakeholderType);
        await dbContext.SaveChangesAsync();

        _countryId = country.Id;
        _stakeholderTypeId = stakeholderType.Id;
    }
}
