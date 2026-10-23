namespace BLVK.Pickly.Logistics.WebAPI.Features.Operations.Dashboard;

/// <summary>
/// Serves the public operations dashboard page. The page is an embedded resource so it ships inside the assembly and
/// behaves the same in every environment; its data comes from the anonymous operations metrics API.
/// </summary>
public static class OperationsDashboardEndpoints
{
    public const string Route = "/operations";
    private const string ResourceName = "OperationsDashboard.html";

    private static readonly Lazy<string> Page = new(() =>
    {
        using var stream = typeof(OperationsDashboardEndpoints).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    });

    public static IEndpointRouteBuilder MapOperationsDashboard(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Route, () => TypedResults.Content(Page.Value, "text/html; charset=utf-8"))
            .AllowAnonymous()
            .ExcludeFromDescription();
        return endpoints;
    }
}
