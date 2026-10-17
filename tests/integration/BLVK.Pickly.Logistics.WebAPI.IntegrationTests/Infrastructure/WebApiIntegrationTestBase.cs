using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

public abstract class WebApiIntegrationTestBase
{
    private readonly CustomWebApplicationFactory _factory;

    protected WebApiIntegrationTestBase(
        ContainersFixture fixture,
        IReadOnlyDictionary<string, string?>? configurationOverrides = null)
    {
        _factory = new CustomWebApplicationFactory(
            fixture.PostgresConnectionString,
            fixture.RedisConnectionString,
            configurationOverrides: configurationOverrides);
    }

    protected HttpClient Client { get; private set; } = default!;
    protected FakeGoogleIdentityTokenService GoogleIdentityTokenService => _factory.GoogleIdentityTokenService;
    protected FakeLocationGeocodingProvider LocationGeocodingProvider => _factory.LocationGeocodingProvider;
    protected FakeDeliveryRouteProvider DeliveryRouteProvider => _factory.DeliveryRouteProvider;

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
}
