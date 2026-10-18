using BLVK.Pickly.Logistics.Domain.Payments.Services;
using BLVK.Pickly.Logistics.Infrastructure.Http;
using BLVK.Pickly.Logistics.Infrastructure.Payments.Credo;
using BLVK.Pickly.Logistics.Infrastructure.Payments.SafeHaven;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace BLVK.Pickly.Logistics.Infrastructure.Payments;

public static class ServiceCollectionExtensions
{
    private static readonly IAsyncPolicy<HttpResponseMessage> TransientReadRetryPolicy =
        HttpRetryPolicies.CreateTransientRetryPolicy();
    private static readonly IAsyncPolicy<HttpResponseMessage> NoRetryPolicy =
        Policy.NoOpAsync<HttpResponseMessage>();

    public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        var safeHavenOptions = configuration.GetSection(SafeHavenOptions.SectionName).Get<SafeHavenOptions>() ?? new SafeHavenOptions();
        var credoOptions = configuration.GetSection(CredoOptions.SectionName).Get<CredoOptions>() ?? new CredoOptions();

        services.Configure<SafeHavenOptions>(configuration.GetSection(SafeHavenOptions.SectionName));
        services.Configure<CredoOptions>(configuration.GetSection(CredoOptions.SectionName));

        services.AddHttpClient(PaymentHttpClientNames.SafeHaven, client =>
        {
            client.BaseAddress = new Uri(safeHavenOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(SelectPaymentHttpPolicy);
        services.AddHttpClient(PaymentHttpClientNames.Credo, client =>
        {
            client.BaseAddress = new Uri(credoOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(10);
        })
        .AddPolicyHandler(SelectPaymentHttpPolicy);

        services.AddScoped<ISafeHavenClient, SafeHavenClient>();
        services.AddScoped<ICredoClient, CredoClient>();
        services.AddScoped<ICredoWebhookSignatureValidator, CredoWebhookSignatureValidator>();
        services.AddScoped<IPaymentProviderService, SafeHavenPaymentProviderService>();
        services.AddScoped<IPaymentProviderService, CredoPaymentProviderService>();

        return services;
    }

    internal static IAsyncPolicy<HttpResponseMessage> SelectPaymentHttpPolicy(HttpRequestMessage request) =>
        request.Method == HttpMethod.Get || request.Method == HttpMethod.Head
            ? TransientReadRetryPolicy
            : NoRetryPolicy;
}
