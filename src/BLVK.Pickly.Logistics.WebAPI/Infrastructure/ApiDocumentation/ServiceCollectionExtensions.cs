using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.ApiDocumentation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        foreach (var version in ApiDocumentationVersions.All)
        {
            services.AddOpenApi(version, options =>
            {
                options.AddDocumentTransformer((document, _, _) =>
                {
                    document.Info ??= new OpenApiInfo();
                    document.Info.Title = "BLVK.Pickly.Logistics.WebAPI";
                    document.Info.Version = version;

                    document.Components ??= new OpenApiComponents();
                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme,
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Name = "Authorization",
                        Description = "Enter a JWT bearer token."
                    };

                    return Task.CompletedTask;
                });
            });
        }

        return services;
    }
}
