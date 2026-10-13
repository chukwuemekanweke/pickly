using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;

public sealed class CustomConsumerApplicationFactory(
    string postgresConnectionString,
    string redisConnectionString,
    string rabbitMqHostName,
    int rabbitMqPort,
    string rabbitMqUserName,
    string rabbitMqPassword,
    string rabbitMqVirtualHost) : WebApplicationFactory<Program>
{
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
                ["Messaging:RabbitMq:ServiceName"] = "BLVK.Pickly.Logistics.Consumer.IntegrationTests",
                ["Messaging:RabbitMq:HostName"] = rabbitMqHostName,
                ["Messaging:RabbitMq:Port"] = rabbitMqPort.ToString(),
                ["Messaging:RabbitMq:UserName"] = rabbitMqUserName,
                ["Messaging:RabbitMq:Password"] = rabbitMqPassword,
                ["Messaging:RabbitMq:VirtualHost"] = rabbitMqVirtualHost,
                ["Messaging:RabbitMq:EventsExchange"] = "x.events.blvkpicklylogistics.integrationtests",
                ["Messaging:RabbitMq:CommandsExchange"] = "x.commands.blvkpicklylogistics.integrationtests",
                ["Notifications:Email:FromAddress"] = "no-reply@integrationtests.local",
                ["Notifications:Email:FromName"] = "BLVK.Pickly.Logistics Integration Tests",
                ["OpenTelemetry:ServiceName"] = "BLVK.Pickly.Logistics.Consumer.IntegrationTests",
                ["OpenTelemetry:OtlpEndpoint"] = "http://localhost:4317"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.AddHostedService<TestReadyWorker>();
        });
    }
}
