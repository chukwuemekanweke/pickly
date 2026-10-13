namespace BLVK.Pickly.Logistics.Observability.IntegrationTests;

[Collection(nameof(ObservabilityCollection))]
public sealed class WhenTailSampling_WithNormalTraceAndFullBaseline_Should(TempoFixture fixture) : IAsyncLifetime
{
    // A very high latency threshold keeps this trace out of the "slow" policy, isolating the probabilistic policy.
    private const int LatencyThresholdMs = 100_000;
    private static readonly TimeSpan RetentionTimeout = TimeSpan.FromSeconds(30);

    private OtelCollectorContainer _collector = default!;

    public async Task InitializeAsync() =>
        _collector = await OtelCollectorContainer.StartAsync(fixture.Network, LatencyThresholdMs, samplingPercentage: 100);

    public async Task DisposeAsync() => await _collector.DisposeAsync();

    [Fact]
    public async Task RetainTheTrace()
    {
        var traceId = await SyntheticTraceEmitter.EmitAsync(_collector.OtlpEndpoint);

        using var tempoClient = fixture.CreateClient();
        var found = await tempoClient.WaitForTraceAsync(traceId, RetentionTimeout);

        found.ShouldBeTrue("normal traffic must pass through the probabilistic policy when the baseline is 100%");
    }
}
