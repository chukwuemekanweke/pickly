using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.Jobs.IntegrationTests.Infrastructure;

public abstract class JobsIntegrationTestBase
{
    private readonly CustomJobsApplicationFactory _factory;

    protected JobsIntegrationTestBase(ContainersFixture fixture)
    {
        _factory = new CustomJobsApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            fixture.RabbitMqHostName,
            fixture.RabbitMqPort,
            fixture.RabbitMqUserName,
            fixture.RabbitMqPassword,
            fixture.RabbitMqVirtualHost);
    }

    protected HttpClient Client { get; private set; } = default!;

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

    protected static async Task WaitForHealthyAsync(Func<Task<HttpResponseMessage>> probe)
    {
        await WaitForConditionAsync(async () =>
        {
            using var response = await probe();
            return response.StatusCode == System.Net.HttpStatusCode.OK;
        });
    }
}
