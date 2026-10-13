using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenTwoFactorVerificationRateLimitIsExceeded_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.CreateVersion7().ToString());
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnTooManyRequests()
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            _response?.Dispose();
            _response = await Client.PostAsJsonAsync(
                EndpointUrl.Sessions.TwoFactorV1,
                new CompleteTwoFactorChallengeRequest("unknown-challenge", "authenticator", "123456"));
        }

        _response.ShouldNotBeNull();
        _response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        var payload = await _response.Content.ReadFromJsonAsync<ProblemDetails>();
        payload.ShouldNotBeNull();
        payload.Status.ShouldBe(StatusCodes.Status429TooManyRequests);
    }
}
