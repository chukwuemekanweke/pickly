using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace BLVK.Pickly.Logistics.Observability.IntegrationTests.Infrastructure;

/// <summary>
/// Starts the real `otel/opentelemetry-collector-contrib` image used by docker-compose, mounting the
/// repository's checked-in `observability/otel-collector/otel-collector.yml` unmodified, so tests exercise
/// the shipped tail-sampling configuration rather than a copy of it.
/// </summary>
public sealed class OtelCollectorContainer : IAsyncDisposable
{
    private const int OtlpGrpcPort = 4317;

    private readonly IContainer _container;

    private OtelCollectorContainer(IContainer container)
    {
        _container = container;
    }

    public string OtlpEndpoint => $"http://localhost:{_container.GetMappedPublicPort(OtlpGrpcPort)}";

    public static async Task<OtelCollectorContainer> StartAsync(INetwork network, int latencyThresholdMs, int samplingPercentage)
    {
        var container = new ContainerBuilder("otel/opentelemetry-collector-contrib:latest")
            .WithNetwork(network)
            .WithCommand("--config=/etc/otel-collector/config.yml")
            .WithBindMount(RepositoryPaths.OtelCollectorConfigPath, "/etc/otel-collector/config.yml", AccessMode.ReadOnly)
            .WithEnvironment("OTEL_TAIL_SAMPLING_LATENCY_MS", latencyThresholdMs.ToString())
            .WithEnvironment("OTEL_TAIL_SAMPLING_PERCENTAGE", samplingPercentage.ToString())
            .WithPortBinding(OtlpGrpcPort, true)
            // A TCP-port check only proves the listener is accepting connections, not that the OTLP
            // receiver behind it is registered yet, which was enough of a race to silently drop the
            // first export. The collector's own readiness log line is the precise signal to wait for.
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilMessageIsLogged("Everything is ready. Begin running and processing data."))
            .Build();

        await container.StartAsync();

        return new OtelCollectorContainer(container);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
