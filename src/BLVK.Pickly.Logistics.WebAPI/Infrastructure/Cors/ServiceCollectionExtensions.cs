using Microsoft.Net.Http.Headers;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.Cors;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredCors(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
            ?? throw new InvalidOperationException($"Configuration section '{CorsSettings.SectionName}' is required.");
        settings.Validate();

        services.Configure<CorsSettings>(configuration.GetSection(CorsSettings.SectionName));
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy
                    .WithOrigins(settings.AllowedOrigins)
                    .WithMethods(
                        HttpMethods.Get,
                        HttpMethods.Head,
                        HttpMethods.Post,
                        HttpMethods.Put,
                        HttpMethods.Patch,
                        HttpMethods.Delete,
                        HttpMethods.Options)
                    .WithHeaders(
                        HeaderNames.Accept,
                        HeaderNames.Authorization,
                        HeaderNames.ContentType,
                        "X-Tenant-Id",
                        "Idempotency-Key");
            });
        });

        return services;
    }
}
