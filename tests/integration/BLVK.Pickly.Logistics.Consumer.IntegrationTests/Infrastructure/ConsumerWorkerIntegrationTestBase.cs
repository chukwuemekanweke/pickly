using BLVK.Pickly.Logistics.Infrastructure.Authentication;
using BLVK.Pickly.Logistics.Infrastructure.Caching;
using BLVK.Pickly.Logistics.Infrastructure.Messaging;
using BLVK.Pickly.Logistics.Infrastructure.Notifications;
using BLVK.Pickly.Logistics.Infrastructure.Observability;
using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using BLVK.Pickly.Logistics.Infrastructure.Storage;
using Chidelu.Integration.Messaging.RabbitMQ.Consumer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;

public abstract class ConsumerWorkerIntegrationTestBase : IAsyncLifetime
{
    private readonly ContainersFixture _fixture;
    private IHost _host = default!;

    protected ConsumerWorkerIntegrationTestBase(ContainersFixture fixture)
    {
        _fixture = fixture;
    }

    public virtual async Task InitializeAsync()
    {
        await InitializeHostAsync();
        await MigrateDatabaseAsync();
        await InitializeWorkerTestAsync();
    }

    public virtual async Task DisposeAsync()
    {
        await DisposeWorkerTestAsync();
        await DisposeHostAsync();
    }

    protected IServiceScope CreateScope() => _host.Services.CreateScope();

    protected ScopedDbContext CreateDbContextScope() => new(_host.Services.CreateScope());

    protected async Task MigrateDatabaseAsync()
    {
        using var scope = CreateDbContextScope();
        await scope.DbContext.Database.MigrateAsync();
    }

    protected async Task DisposeHostAsync()
    {
        if (_host is null)
        {
            return;
        }

        await _host.StopAsync();
        _host.Dispose();
    }

    protected virtual Task InitializeWorkerTestAsync() => Task.CompletedTask;

    protected virtual Task DisposeWorkerTestAsync() => Task.CompletedTask;

    protected virtual void RegisterTestServices(IServiceCollection services)
    {
        services.RemoveAll<IMessageContext>();
        var messageContext = Substitute.For<IMessageContext>();
        messageContext.CorrelationId.Returns(Guid.CreateVersion7().ToString("N"));
        services.AddSingleton(messageContext);
    }

    protected virtual IReadOnlyDictionary<string, string?> GetConfigurationOverrides() =>
        new Dictionary<string, string?>();

    protected static async Task WaitForConditionAsync(Func<Task<bool>> condition)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new InvalidOperationException("The expected condition was not met in time.");
    }

    private async Task InitializeHostAsync()
    {
        var configurationValues = new Dictionary<string, string?>
        {
                ["ConnectionStrings:PostgresWrite"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = _fixture.PostgresConnectionString,
                ["ConnectionStrings:Redis"] = _fixture.RedisConnectionString,
                ["Messaging:RabbitMq:ServiceName"] = "BLVK.Pickly.Logistics.Consumer.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = _fixture.RabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = _fixture.RabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = _fixture.RabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = _fixture.RabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = _fixture.RabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = "x.events.blvkpicklylogistics.integrationtests",
                ["Messaging:RabbitMq:CommandsExchange"] = "x.commands.blvkpicklylogistics.integrationtests",
                ["Notifications:Email:FromAddress"] = "no-reply@integrationtests.local",
                ["Notifications:Email:FromName"] = "BLVK.Pickly.Logistics Integration Tests",
                ["OpenTelemetry:ServiceName"] = "BLVK.Pickly.Logistics.Consumer.IntegrationTests",
            ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
        };
        foreach (var entry in GetConfigurationOverrides())
        {
            configurationValues[entry.Key] = entry.Value;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddLogging();
        builder.Services.AddDataProtection();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<WorkerReadinessState>();
        builder.Services.AddPostgresPersistence(configuration);
        builder.Services.AddIdentityUserManagement(configuration);
        builder.Services.AddAuthenticationServices();
        builder.Services.AddRedisCaching(configuration);
        builder.Services.AddTransactionalOutbox();
        builder.Services.AddNotificationServices(configuration);
        builder.Services.AddObjectStorage(configuration);
        builder.Services.AddCustomTelemetryContext();
        builder.Services.AddBackendTelemetry(configuration);
        builder.Services.AddSubscribers(configuration);
        RegisterTestServices(builder.Services);

        _host = builder.Build();
        await _host.StartAsync();
    }
}
