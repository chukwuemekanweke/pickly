using BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.HealthChecks;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCheckingReadinessWithAvailableDependencies_Should(ContainersFixture fixture)
    : JobsIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string ReadinessEndpoint = "/health/readiness";
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        await WaitForDependenciesToBecomeHealthyAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnHealthy()
    {
        await WhenCheckingReadiness();
        ThenTheEndpointIsHealthy();

        async Task WhenCheckingReadiness()
        {
            _response = await Client.GetAsync(ReadinessEndpoint);
        }

        void ThenTheEndpointIsHealthy()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    private Task WaitForDependenciesToBecomeHealthyAsync() =>
        WaitForHealthyAsync(() => Client.GetAsync(ReadinessEndpoint));
}

