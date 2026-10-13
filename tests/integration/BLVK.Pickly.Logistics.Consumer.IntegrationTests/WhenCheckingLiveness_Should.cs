using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCheckingLiveness_Should(ContainersFixture fixture)
    : ConsumerIntegrationTestBase(fixture)
{
    private const string LivenessEndpoint = "/health/liveness";
    private HttpResponseMessage? _response;

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

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
    }
}

