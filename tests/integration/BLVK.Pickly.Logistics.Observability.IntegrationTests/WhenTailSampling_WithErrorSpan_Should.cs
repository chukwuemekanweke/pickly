namespace BLVK.Pickly.Logistics.Observability.IntegrationTests;

[Collection(nameof(ObservabilityCollection))]
public sealed class WhenTailSampling_WithErrorSpan_Should(TempoFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan RetentionTimeout = TimeSpan.FromSeconds(30);

    private OtelCollectorContainer _collector = default!;

    public async Task InitializeAsync() =>
        // A zero baseline proves retention is coming from the error policy, not from probabilistic luck.
        _collector = await OtelCollectorContainer.StartAsync(fixture.Network, latencyThresholdMs: 1000, samplingPercentage: 0);

    public async Task DisposeAsync() => await _collector.DisposeAsync();

    [Fact]
    public async Task RetainTheTrace()
    {
        var traceId = await SyntheticTraceEmitter.EmitAsync(_collector.OtlpEndpoint, isError: true);

        using var tempoClient = fixture.CreateClient();
        var found = await tempoClient.WaitForTraceAsync(traceId, RetentionTimeout);

        found.ShouldBeTrue("a trace containing an ERROR span must be retained regardless of the probabilistic baseline");
    }
}
