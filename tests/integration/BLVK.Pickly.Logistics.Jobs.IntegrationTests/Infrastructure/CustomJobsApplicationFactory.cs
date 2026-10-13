using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;

public sealed class CustomJobsApplicationFactory(
    string postgresConnectionString,
    string redisConnectionString,
    string rabbitMqHostName,
    int rabbitMqPort,
    string rabbitMqUserName,
    string rabbitMqPassword,
    string rabbitMqVirtualHost) : WebApplicationFactory<Program>
{
    public const string EventsExchange = "x.events.blvkpicklylogistics.integrationtests";
    public const string CommandsExchange = "x.commands.blvkpicklylogistics.integrationtests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresWrite"] = postgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = postgresConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Messaging:RabbitMq:ServiceName"] = "BLVK.Pickly.Logistics.Jobs.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = rabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = rabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = rabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = rabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = rabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = EventsExchange,
                ["Messaging:RabbitMq:CommandsExchange"] = CommandsExchange,
                [$"{BLVK.Pickly.Logistics.Jobs.OutboxProcessing.OutboxProcessingOptions.SectionName}:PollIntervalSeconds"] = "1",
                ["OpenTelemetry:ServiceName"] = "BLVK.Pickly.Logistics.Jobs.IntegrationTests",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
            });
        });
    }
}
