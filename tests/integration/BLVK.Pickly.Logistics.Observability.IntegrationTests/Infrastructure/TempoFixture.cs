using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Xunit.Sdk;

namespace BLVK.Pickly.Logistics.Observability.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(ObservabilityCollection))]
public sealed class ObservabilityCollection : ICollectionFixture<TempoFixture>;

public sealed class TempoFixture : IAsyncLifetime
{
    public const int TempoHttpPort = 3200;
    public const string TempoNetworkAlias = "tempo";

    private INetwork _network = default!;
    private IContainer _tempo = default!;

    public INetwork Network => _network;

    public TempoClient CreateClient() =>
        new($"http://localhost:{_tempo.GetMappedPublicPort(TempoHttpPort)}");

    public async Task InitializeAsync()
    {
        try
        {
            _network = new NetworkBuilder().Build();
            await _network.CreateAsync();

            _tempo = new ContainerBuilder("grafana/tempo:2.8.1")
                .WithNetwork(_network)
                .WithNetworkAliases(TempoNetworkAlias)
                .WithCommand("-config.file=/etc/tempo.yml")
                .WithBindMount(RepositoryPaths.TempoConfigPath, "/etc/tempo.yml", AccessMode.ReadOnly)
                .WithPortBinding(TempoHttpPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(request => request.ForPort(TempoHttpPort).ForPath("/ready")))
                .Build();

            await _tempo.StartAsync();
        }
        catch (Exception exception) when (exception.GetType().FullName?.Contains("DockerUnavailableException", StringComparison.Ordinal) == true)
        {
            throw SkipException.ForSkip("Docker is unavailable. Observability integration tests require Docker to run.");
        }
    }

    public async Task DisposeAsync()
    {
        if (_tempo is not null)
        {
            await _tempo.DisposeAsync();
        }

        if (_network is not null)
        {
            await _network.DeleteAsync();
        }
    }
}
