using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Observability;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Jobs.Infrastructure.BackgroundServices;
using BLVK.Pickly.Logistics.Jobs.OutboxProcessing;
using BLVK.Pickly.Logistics.Jobs.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;

public abstract class JobsWorkerIntegrationTestBase : IAsyncLifetime
{
    private readonly ContainersFixture _fixture;
    private IHost? _workerHost;

    protected JobsWorkerIntegrationTestBase(ContainersFixture fixture)
    {
        _fixture = fixture;
    }

    private void InitializeWorkerHost(Action<IServiceCollection, IConfiguration> registerWorkers)
    {
        var configuration = BuildConfiguration();
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddLogging();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddBackgroundServiceReadinessTracking();
        builder.Services.AddCustomTelemetryContext();
        builder.Services.AddPostgresPersistence(configuration);
        builder.Services.AddRabbitMqOutboxDispatching(configuration);
        registerWorkers(builder.Services, configuration);

        _workerHost = builder.Build();
    }

    protected async Task StartWorkerHostAsync()
    {
        if (_workerHost is null)
        {
            throw new InvalidOperationException("The worker host has not been initialized.");
        }

        await _workerHost.StartAsync();
    }

    protected async Task DisposeWorkerHostAsync()
    {
        if (_workerHost is null)
        {
            return;
        }

        await _workerHost.StopAsync();
        _workerHost.Dispose();
    }

    protected async Task MigrateJobsDatabaseAsync()
    {
        await using var scope = CreateDbContextScope();
        var dbContext = scope.DbContext;
        await dbContext.Database.MigrateAsync();
    }

    protected ScopedDbContext CreateDbContextScope()
    {
        if (_workerHost is null)
        {
            throw new InvalidOperationException("The worker host has not been initialized.");
        }

        return new ScopedDbContext(_workerHost.Services.CreateAsyncScope());
    }

    protected static Task WaitForConditionAsync(Func<Task<bool>> condition) =>
        WaitForConditionAsync(condition, TimeSpan.FromSeconds(30));

    protected static async Task WaitForConditionAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadlineUtc = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadlineUtc)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException($"The expected condition was not met within {timeout}.");
    }

    public virtual async Task InitializeAsync()
    {
        InitializeWorkerHost(RegisterWorkers);
        await MigrateJobsDatabaseAsync();
        await InitializeWorkerTestAsync();
        await StartWorkerHostAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await DisposeWorkerTestAsync();
        await DisposeWorkerHostAsync();
    }

    protected virtual Task InitializeWorkerTestAsync() => Task.CompletedTask;

    protected virtual Task DisposeWorkerTestAsync() => Task.CompletedTask;

    protected abstract void RegisterWorkers(IServiceCollection services, IConfiguration configuration);

    protected virtual IReadOnlyDictionary<string, string?> GetAdditionalConfiguration() =>
        new Dictionary<string, string?>();

    private IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(BuildConfigurationValues())
            .Build();

    private IReadOnlyDictionary<string, string?> BuildConfigurationValues()
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgresWrite"] = _fixture.PostgresConnectionString,
            ["ConnectionStrings:PostgresRead"] = _fixture.PostgresConnectionString,
            ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
            [$"{OutboxProcessingOptions.SectionName}:BatchSize"] = "50",
            [$"{OutboxProcessingOptions.SectionName}:PollIntervalSeconds"] = "1",
            [$"{PaymentReconciliationOptions.SectionName}:StaleThreshold"] = "00:00:00",
            [$"{PaymentReconciliationOptions.SectionName}:PollInterval"] = "00:00:01",
            ["Messaging:RabbitMq:ServiceName"] = "BLVK.Pickly.Logistics.Jobs.IntegrationTests",
            ["Messaging:RabbitMq:HostName"] = _fixture.RabbitMqHostName,
            ["Messaging:RabbitMq:Port"] = _fixture.RabbitMqPort.ToString(),
            ["Messaging:RabbitMq:UserName"] = _fixture.RabbitMqUserName,
            ["Messaging:RabbitMq:Password"] = _fixture.RabbitMqPassword,
            ["Messaging:RabbitMq:VirtualHost"] = _fixture.RabbitMqVirtualHost,
            ["Messaging:RabbitMq:EventsExchange"] = CustomJobsApplicationFactory.EventsExchange,
            ["Messaging:RabbitMq:CommandsExchange"] = CustomJobsApplicationFactory.CommandsExchange,
            ["OpenTelemetry:ServiceName"] = "BLVK.Pickly.Logistics.Jobs.IntegrationTests",
            ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
        };

        foreach (var pair in GetAdditionalConfiguration())
        {
            values[pair.Key] = pair.Value;
        }

        return values;
    }
}
