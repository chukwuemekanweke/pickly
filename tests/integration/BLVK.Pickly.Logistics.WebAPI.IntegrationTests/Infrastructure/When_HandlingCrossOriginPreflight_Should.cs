using Microsoft.AspNetCore.Http;
using Shouldly;
using System.Net;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;

[Collection(nameof(ContainersCollection))]
public sealed class When_HandlingCrossOriginPreflight_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private const string AllowedOrigin = "https://app.pikly.org";

    public Task InitializeAsync() => InitializeClientAsync();

    public Task DisposeAsync() => DisposeClientAsync();

    [Fact]
    public async Task AllowConfiguredFrontendOrigin()
    {
        using var request = CreatePreflightRequest(AllowedOrigin, HttpMethods.Post, "authorization");

        using var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Single().ShouldBe(AllowedOrigin);
        response.Headers.Contains("Access-Control-Allow-Credentials").ShouldBeFalse();
    }

    [Fact]
    public async Task AllowRequiredHeadersAndMethods()
    {
        const string requestedHeaders = "authorization,content-type,x-tenant-id,idempotency-key";
        using var request = CreatePreflightRequest(AllowedOrigin, HttpMethods.Patch, requestedHeaders);

        using var response = await Client.SendAsync(request);

        var allowedHeaders = ReadCommaSeparatedHeader(response, "Access-Control-Allow-Headers");
        var allowedMethods = ReadCommaSeparatedHeader(response, "Access-Control-Allow-Methods");

        allowedHeaders.ShouldContain("Authorization", StringComparer.OrdinalIgnoreCase);
        allowedHeaders.ShouldContain("Content-Type", StringComparer.OrdinalIgnoreCase);
        allowedHeaders.ShouldContain("X-Tenant-Id", StringComparer.OrdinalIgnoreCase);
        allowedHeaders.ShouldContain("Idempotency-Key", StringComparer.OrdinalIgnoreCase);
        allowedMethods.ShouldContain(HttpMethods.Get, StringComparer.OrdinalIgnoreCase);
        allowedMethods.ShouldContain(HttpMethods.Post, StringComparer.OrdinalIgnoreCase);
        allowedMethods.ShouldContain(HttpMethods.Put, StringComparer.OrdinalIgnoreCase);
        allowedMethods.ShouldContain(HttpMethods.Patch, StringComparer.OrdinalIgnoreCase);
        allowedMethods.ShouldContain(HttpMethods.Delete, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OmitCorsHeadersForDisallowedOrigin()
    {
        using var request = CreatePreflightRequest("https://unrelated.example", HttpMethods.Post, "authorization");

        using var response = await Client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
        response.Headers.Contains("Access-Control-Allow-Headers").ShouldBeFalse();
        response.Headers.Contains("Access-Control-Allow-Methods").ShouldBeFalse();
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin, string method, string requestedHeaders)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        request.Headers.Add("Access-Control-Request-Headers", requestedHeaders);
        return request;
    }

    private static string[] ReadCommaSeparatedHeader(HttpResponseMessage response, string name) =>
        response.Headers
            .GetValues(name)
            .SelectMany(value => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
}
