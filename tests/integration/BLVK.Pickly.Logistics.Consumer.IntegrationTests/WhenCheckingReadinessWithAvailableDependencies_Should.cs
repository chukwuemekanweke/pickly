using BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests;

[Collection(nameof(ContainersCollection))]
public sealed class WhenCheckingReadinessWithAvailableDependencies_Should(ContainersFixture fixture)
    : ConsumerIntegrationTestBase(fixture)
{
    private const string ReadinessEndpoint = "/health/readiness";
    private HttpResponseMessage? _response;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await WaitForDependenciesToBecomeHealthyAsync();
    }

    public override async Task DisposeAsync()
    {
        _response?.Dispose();
        await base.DisposeAsync();
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

