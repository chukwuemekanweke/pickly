using BLVK.Pickly.Logistics.Domain.Common.Authentication;
using BLVK.Pickly.Logistics.Domain.Deliveries.Services;
using BLVK.Pickly.Logistics.Domain.Locations.Services;
using BLVK.Pickly.Logistics.Domain.Payments.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory(
    string postgresConnectionString,
    string redisConnectionString,
    bool useFakePaymentProviderServices = true,
    IReadOnlyDictionary<string, string?>? configurationOverrides = null) : WebApplicationFactory<Program>
{
    public FakeGoogleIdentityTokenService GoogleIdentityTokenService { get; } = new();
    public FakeLocationGeocodingProvider LocationGeocodingProvider { get; } = new();
    public FakeDeliveryRouteProvider DeliveryRouteProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var configuration = new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgresWrite"] = postgresConnectionString,
                ["ConnectionStrings:PostgresRead"] = postgresConnectionString,
                ["ConnectionStrings:Redis"] = redisConnectionString,
                ["Database:InitializeOnStartup"] = "true",
                ["Jwt:Issuer"] = "integration-tests",
                ["Jwt:Audience"] = "integration-tests",
                ["Jwt:SigningKey"] = "super-secret-template-signing-key-change-me",
                ["Authentication:Google:ClientIds:0"] = "integration-tests-google-client-id",
                ["Authentication:Google:Enabled"] = "true",
                ["Payments:Credo:BaseUrl"] = "https://credo.integration.local",
                ["Payments:Credo:PublicKey"] = "0PUB_test_public_key",
                ["Payments:Credo:SecretKey"] = "test_secret_key",
                ["Payments:Credo:CallbackUrl"] = "https://backend.integration.local/payments/webhooks/credo",
                ["Payments:SafeHaven:BaseUrl"] = "https://safehaven.integration.local",
                ["Payments:SafeHaven:ClientId"] = "integration-tests-client-id",
                ["Payments:SafeHaven:ClientAssertion"] = "integration-tests-client-assertion",
                ["Payments:SafeHaven:CallbackUrl"] = "https://backend.integration.local/payments/webhooks/safehaven",
                ["Payments:SafeHaven:AutoSweepAccountNumber"] = "1234567890",
                ["Payments:SafeHaven:ValidFor"] = "900",
                ["Payments:SafeHaven:SettlementBankCode"] = "090286",
                ["Payments:SafeHaven:SettlementAccountNumber"] = "9876543210",
                ["OpenTelemetry:ServiceName"] = "BLVK.Pickly.Logistics.WebAPI.Tests",
                ["OpenTelemetry:OtlpEndpoint"] = ""
            };

            if (configurationOverrides is not null)
            {
                foreach (var entry in configurationOverrides)
                {
                    configuration[entry.Key] = entry.Value;
                }
            }

            configBuilder.AddInMemoryCollection(configuration);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGoogleIdentityTokenService>();
            services.AddSingleton<IGoogleIdentityTokenService>(GoogleIdentityTokenService);
            services.RemoveAll<ILocationGeocodingProvider>();
            services.AddSingleton<ILocationGeocodingProvider>(LocationGeocodingProvider);
            services.RemoveAll<IDeliveryRouteProvider>();
            services.AddSingleton<IDeliveryRouteProvider>(DeliveryRouteProvider);

            if (useFakePaymentProviderServices)
            {
                services.RemoveAll<IPaymentProviderService>();
                services.AddScoped<IPaymentProviderService, FakeCredoPaymentProviderService>();
                services.AddScoped<IPaymentProviderService, FakeSafeHavenPaymentProviderService>();
            }
            else if (configurationOverrides is not null)
            {
                if (configurationOverrides.TryGetValue("Payments:SafeHaven:BaseUrl", out var safeHavenBaseUrl) &&
                    !string.IsNullOrWhiteSpace(safeHavenBaseUrl))
                {
                    services.AddHttpClient("payments-safehaven", client =>
                    {
                        client.BaseAddress = new Uri(safeHavenBaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(10);
                    });
                }

                if (configurationOverrides.TryGetValue("Payments:Credo:BaseUrl", out var credoBaseUrl) &&
                    !string.IsNullOrWhiteSpace(credoBaseUrl))
                {
                    services.AddHttpClient("payments-credo", client =>
                    {
                        client.BaseAddress = new Uri(credoBaseUrl);
                        client.Timeout = TimeSpan.FromSeconds(10);
                    });
                }
            }

            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<IDistributedCache>();

            var redisConfiguration = ConfigurationOptions.Parse(redisConnectionString);
            redisConfiguration.AbortOnConnectFail = false;
            var lazyMultiplexer = new Lazy<IConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(redisConfiguration));
            services.AddSingleton<IConnectionMultiplexer>(_ => lazyMultiplexer.Value);
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = () => Task.FromResult(lazyMultiplexer.Value);
            });
        });
    }
}
