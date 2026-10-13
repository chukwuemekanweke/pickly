using BLVK.Pickly.Logistics.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace BLVK.Pickly.Logistics.Consumer;

public sealed class ConsumerReadinessHealthCheck(
    IConfiguration configuration,
    WorkerReadinessState readinessState) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!readinessState.IsReady)
        {
            return HealthCheckResult.Unhealthy("Consumer worker has not started yet.");
        }

        try
        {
            var sqlWriteConnectionString = GetRequiredConnectionString("PostgresWrite");
            var sqlReadConnectionString = GetRequiredConnectionString("PostgresRead");
            var redisConnectionString = GetRequiredConnectionString("Redis");
            var rabbitMqHostName = GetRequiredConfigurationValue("Messaging:RabbitMq:HostName");
            var rabbitMqUserName = GetRequiredConfigurationValue("Messaging:RabbitMq:UserName");
            var rabbitMqPassword = GetRequiredConfigurationValue("Messaging:RabbitMq:Password");
            var rabbitMqVirtualHost = GetRequiredConfigurationValue("Messaging:RabbitMq:VirtualHost");
            var rabbitMqPort = GetRequiredPort("Messaging:RabbitMq:Port");

            await using var sqlWriteConnection = new NpgsqlConnection(sqlWriteConnectionString);
            await sqlWriteConnection.OpenAsync(cancellationToken);

            await using var sqlWriteCommand = new NpgsqlCommand("SELECT 1", sqlWriteConnection);
            await sqlWriteCommand.ExecuteScalarAsync(cancellationToken);

            await using var sqlReadConnection = new NpgsqlConnection(sqlReadConnectionString);
            await sqlReadConnection.OpenAsync(cancellationToken);

            await using var sqlReadCommand = new NpgsqlCommand("SELECT 1", sqlReadConnection);
            await sqlReadCommand.ExecuteScalarAsync(cancellationToken);

            await using var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
            await redis.GetDatabase().PingAsync();

            var connectionFactory = new ConnectionFactory
            {
                HostName = rabbitMqHostName,
                Port = rabbitMqPort,
                UserName = rabbitMqUserName,
                Password = rabbitMqPassword,
                VirtualHost = rabbitMqVirtualHost
            };

            await using var rabbitMqConnection = await connectionFactory.CreateConnectionAsync(cancellationToken);

            return HealthCheckResult.Healthy("Consumer worker can connect to write PostgreSQL, read PostgreSQL, Redis, and RabbitMQ.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "Consumer worker cannot connect to one or more required dependencies.",
                exception);
        }
    }

    private string GetRequiredConnectionString(string name) =>
        PostgresConnectionString.Normalize(
            configuration.GetConnectionString(name)
            ?? throw new InvalidOperationException($"Connection string '{name}' is required."));

    private string GetRequiredConfigurationValue(string key) =>
        configuration[key]
        ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");

    private int GetRequiredPort(string key)
    {
        var value = configuration[key]
            ?? throw new InvalidOperationException($"Configuration value '{key}' is required.");

        return int.TryParse(value, out var port) && port > 0
            ? port
            : throw new InvalidOperationException($"Configuration value '{key}' must be a positive integer.");
    }
}
