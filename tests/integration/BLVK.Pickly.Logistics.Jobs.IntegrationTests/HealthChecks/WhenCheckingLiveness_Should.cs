using BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.HealthChecks;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCheckingLiveness_Should(ContainersFixture fixture)
    : JobsIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string LivenessEndpoint = "/health/liveness";
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnHealthy()
    {
        await WhenCheckingLiveness();
        ThenTheEndpointIsHealthy();

        async Task WhenCheckingLiveness()
        {
            _response = await Client.GetAsync(LivenessEndpoint);
        }

        void ThenTheEndpointIsHealthy()
        {
            _response.ShouldNotBeNull();
            _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}

