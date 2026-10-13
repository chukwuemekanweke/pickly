namespace BLVK.Pickly.Logistics.Observability.IntegrationTests;

[Collection(nameof(ObservabilityCollection))]
public sealed class WhenTailSampling_WithNormalTraceAndZeroBaseline_Should(TempoFixture fixture) : IAsyncLifetime
{
    // A very high latency threshold keeps this trace out of the "slow" policy, isolating the probabilistic policy.
    private const int LatencyThresholdMs = 100_000;
    private static readonly TimeSpan SettleWindow = TimeSpan.FromSeconds(15);

    private OtelCollectorContainer _collector = default!;

    public async Task InitializeAsync() =>
        _collector = await OtelCollectorContainer.StartAsync(fixture.Network, LatencyThresholdMs, samplingPercentage: 0);

    public async Task DisposeAsync() => await _collector.DisposeAsync();

    [Fact]
    public async Task DropTheTrace()
    {
        var traceId = await SyntheticTraceEmitter.EmitAsync(_collector.OtlpEndpoint);

        using var tempoClient = fixture.CreateClient();
        var remainedAbsent = await tempoClient.ConfirmTraceRemainsAbsentAsync(traceId, SettleWindow);

        remainedAbsent.ShouldBeTrue("normal, non-error, non-slow traffic must not survive a 0% probabilistic baseline");
    }
}
