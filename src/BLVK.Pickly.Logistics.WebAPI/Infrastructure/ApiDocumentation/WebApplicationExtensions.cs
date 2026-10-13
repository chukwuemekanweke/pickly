using Scalar.AspNetCore;

namespace BLVK.Pickly.Logistics.WebAPI.Infrastructure.ApiDocumentation;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi("/openapi/{documentName}.json");

        app.MapScalarApiReference("/scalar", options =>
        {
            options.WithTitle("BLVK.Pickly.Logistics.WebAPI");
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
            options.AddDocuments(ApiDocumentationVersions.All);
            options.AddPreferredSecuritySchemes("Bearer");
            options.WithPersistentAuthentication(true);
        })
        .AllowAnonymous();

        return app;
    }
}
