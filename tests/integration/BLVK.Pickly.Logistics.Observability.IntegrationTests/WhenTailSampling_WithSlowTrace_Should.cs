namespace BLVK.Pickly.Logistics.Observability.IntegrationTests;

[Collection(nameof(ObservabilityCollection))]
public sealed class WhenTailSampling_WithSlowTrace_Should(TempoFixture fixture) : IAsyncLifetime
{
    private const int LatencyThresholdMs = 200;
    private static readonly TimeSpan RetentionTimeout = TimeSpan.FromSeconds(30);

    private OtelCollectorContainer _collector = default!;

    public async Task InitializeAsync() =>
        // A zero baseline proves retention is coming from the latency policy, not from probabilistic luck.
        _collector = await OtelCollectorContainer.StartAsync(fixture.Network, LatencyThresholdMs, samplingPercentage: 0);

    public async Task DisposeAsync() => await _collector.DisposeAsync();

    [Fact]
    public async Task RetainTheTrace()
    {
        var traceId = await SyntheticTraceEmitter.EmitAsync(
            _collector.OtlpEndpoint,
            duration: TimeSpan.FromMilliseconds(LatencyThresholdMs * 3));

        using var tempoClient = fixture.CreateClient();
        var found = await tempoClient.WaitForTraceAsync(traceId, RetentionTimeout);

        found.ShouldBeTrue("a trace exceeding the configured latency threshold must be retained regardless of the probabilistic baseline");
    }
}
