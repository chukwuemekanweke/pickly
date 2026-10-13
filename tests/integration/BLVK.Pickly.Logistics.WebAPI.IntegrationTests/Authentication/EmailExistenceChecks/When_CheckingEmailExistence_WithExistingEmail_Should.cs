using BLVK.Pickly.Logistics.Application.Authentication.Features.CheckEmailExistence;
using BLVK.Pickly.Logistics.Domain.Authentication.Entities;
using BLVK.Pickly.Logistics.Domain.Authentication.Persistence;
using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Common.Persistence;
using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.EmailExistenceChecks;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class When_CheckingEmailExistence_WithExistingEmail_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private string _email = string.Empty;
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.CreateVersion7().ToString());
        _email = WebApiIntegrationTestData.Email();

        using var scope = CreateScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IAuthenticationIdentityService>();
        var result = await identityService.CreateAsync(AppUser.Create(_email));
        result.Succeeded.ShouldBeTrue();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();

        using var scope = CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAppUserRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await repository.GetByEmailAsync(_email);
        if (user is not null)
        {
            repository.Remove(user);
            await unitOfWork.SaveChangesAsync();
        }

        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnExistsWithoutAuthentication()
    {
        CheckEmailExistenceResponse? payload = default;

        await WhenCheckingEmailExistence();
        ThenExistsIsReturned();

        async Task WhenCheckingEmailExistence()
        {
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.EmailExistenceChecks.V1,
                new EmailExistenceCheckRequest(_email));
            payload = await _response.Content.ReadFromJsonAsync<CheckEmailExistenceResponse>();
        }

        void ThenExistsIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
            payload.ShouldNotBeNull();
            payload.Exists.ShouldBeTrue();
        }
    }
}
