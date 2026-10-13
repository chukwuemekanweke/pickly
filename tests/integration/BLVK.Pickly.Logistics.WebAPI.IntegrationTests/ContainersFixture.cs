using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit.Sdk;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests;

[CollectionDefinition(nameof(ContainersCollection))]
public sealed class ContainersCollection : ICollectionFixture<ContainersFixture>;

public sealed class ContainersFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = default!;
    public RedisContainer Redis { get; private set; } = default!;
    public string PostgresConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        try
        {
            var databaseName = $"backend_template_webapi_tests_{Guid.CreateVersion7():N}";

            Postgres = new PostgreSqlBuilder("postgres:15.1")
                .WithDatabase(databaseName)
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            Redis = new RedisBuilder("redis:7.0").Build();

            await Task.WhenAll(Postgres.StartAsync(), Redis.StartAsync());

            PostgresConnectionString = Postgres.GetConnectionString();
            RedisConnectionString = Redis.GetConnectionString();
        }
        catch (Exception exception) when (exception.GetType().FullName?.Contains("DockerUnavailableException", StringComparison.Ordinal) == true)
        {
            throw SkipException.ForSkip("Docker is unavailable. WebAPI integration tests require Docker to run.");
        }
    }

    public async Task DisposeAsync()
    {
        var tasks = new List<Task>(2);

        if (Redis is not null)
        {
            tasks.Add(Redis.DisposeAsync().AsTask());
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
