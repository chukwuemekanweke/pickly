using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenAuthPublicRateLimitIsExceeded_Should(ContainersFixture fixture)
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
        ProblemDetails? payload = default;

        await WhenTheSignInLimitIsExceeded();
        await ThenTooManyRequestsIsReturned();

        async Task WhenTheSignInLimitIsExceeded()
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                _response?.Dispose();
                _response = await Client.PostAsJsonAsync(
                    EndpointUrl.Sessions.V1,
                    new SignInRequest(
                        "missing@example.com",
                        "WrongPassword123!"));
            }

            payload = await _response!.Content.ReadFromJsonAsync<ProblemDetails>();
        }

        Task ThenTooManyRequestsIsReturned()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
            payload.ShouldNotBeNull();
            payload.Title.ShouldBe("Too many requests");
            payload.Detail.ShouldBe("Too many requests were sent in a short period. Please wait and try again.");
            payload.Status.ShouldBe(StatusCodes.Status429TooManyRequests);

            return Task.CompletedTask;
        }
    }
}

