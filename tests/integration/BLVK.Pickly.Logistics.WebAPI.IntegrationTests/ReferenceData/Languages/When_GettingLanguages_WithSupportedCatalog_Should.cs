using BLVK.Pickly.Logistics.Application.ReferenceData.Features.GetLanguages;
using BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.ReferenceData.Languages;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingLanguages_WithSupportedCatalog_Should(ContainersFixture fixture)
    : WebApiIntegrationTestBase(fixture), IAsyncLifetime
{
    private HttpResponseMessage? _response;

    public async Task InitializeAsync()
    {
        await InitializeClientAsync();
        Client.DefaultRequestHeaders.Add("X-Tenant-Id", Guid.CreateVersion7().ToString());
    }

    public async Task DisposeAsync()
    {
        _response?.Dispose();
        await DisposeClientAsync();
    }

    [Fact]
    public async Task ReturnOnlyEnglishAndFrench()
    {
        _response = await Client.GetAsync(EndpointUrl.Languages.V1);
        var payload = await _response.Content.ReadFromJsonAsync<IReadOnlyList<GetLanguagesResponse>>();

        _response.StatusCode.ShouldBe(HttpStatusCode.OK);
        payload.ShouldNotBeNull();
        payload.Select(language => language.Code).ShouldBe(["en", "fr"]);
    }
}
