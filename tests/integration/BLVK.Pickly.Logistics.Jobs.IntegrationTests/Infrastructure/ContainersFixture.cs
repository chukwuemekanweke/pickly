using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit.Sdk;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;

[CollectionDefinition(nameof(ContainersCollection))]
public sealed class ContainersCollection : ICollectionFixture<ContainersFixture>;

public sealed class ContainersFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = default!;
    public RabbitMqContainer RabbitMq { get; private set; } = default!;
    public RedisContainer Redis { get; private set; } = default!;
    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;
    public string RabbitMqHostName { get; private set; } = string.Empty;
    public int RabbitMqPort { get; private set; }
    public string RabbitMqUserName => RabbitMqBuilder.DefaultUsername;
    public string RabbitMqPassword => RabbitMqBuilder.DefaultPassword;
    public string RabbitMqVirtualHost => "/";

    public async Task InitializeAsync()
    {
        try
        {
            var databaseName = $"backend_template_jobs_tests_{Guid.CreateVersion7():N}";

            Postgres = new PostgreSqlBuilder("postgis/postgis:16-3.5-alpine")
                .WithDatabase(databaseName)
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            RabbitMq = new RabbitMqBuilder("rabbitmq:3.13-management")
                .WithEnvironment("RABBITMQ_DEFAULT_VHOST", RabbitMqVirtualHost)
                .Build();

            Redis = new RedisBuilder("redis:7.0").Build();

            await Task.WhenAll(Postgres.StartAsync(), RabbitMq.StartAsync(), Redis.StartAsync());

            PostgresConnectionString = Postgres.GetConnectionString();
            RedisConnectionString = Redis.GetConnectionString();
            var rabbitMqConnectionString = new Uri(RabbitMq.GetConnectionString());
            RabbitMqHostName = rabbitMqConnectionString.Host;
            RabbitMqPort = rabbitMqConnectionString.Port;
        }
        catch (Exception exception) when (exception.GetType().FullName?.Contains("DockerUnavailableException", StringComparison.Ordinal) == true)
        {
            throw SkipException.ForSkip("Docker is unavailable. Jobs integration tests require Docker to run.");
        }
    }

    public async Task DisposeAsync()
    {
        var tasks = new List<Task>(3);

        if (Redis is not null)
        {
            tasks.Add(Redis.DisposeAsync().AsTask());
        }

        if (RabbitMq is not null)
        {
            tasks.Add(RabbitMq.DisposeAsync().AsTask());
        }

        if (Postgres is not null)
        {
            tasks.Add(Postgres.DisposeAsync().AsTask());
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
}
