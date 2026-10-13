using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Consumer.IntegrationTests.Infrastructure;

public abstract class ConsumerIntegrationTestBase : IAsyncLifetime
{
    private readonly CustomConsumerApplicationFactory _factory;

    protected ConsumerIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomConsumerApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            fixture.RabbitMqHostName,
            fixture.RabbitMqPort,
            fixture.RabbitMqUserName,
            fixture.RabbitMqPassword,
            fixture.RabbitMqVirtualHost);
    }

    protected HttpClient Client { get; private set; } = default!;

    public virtual Task InitializeAsync() => InitializeClientAsync();

    public virtual Task DisposeAsync() => DisposeClientAsync();

    protected Task InitializeClientAsync()
    {
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return Task.CompletedTask;
    }

    protected async Task DisposeClientAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }

    protected IServiceScope CreateScope() => _factory.Services.CreateScope();

    protected static async Task WaitForHealthyAsync(Func<Task<HttpResponseMessage>> probe)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            using var response = await probe();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new InvalidOperationException("The health endpoint did not become healthy in time.");
    }
}
