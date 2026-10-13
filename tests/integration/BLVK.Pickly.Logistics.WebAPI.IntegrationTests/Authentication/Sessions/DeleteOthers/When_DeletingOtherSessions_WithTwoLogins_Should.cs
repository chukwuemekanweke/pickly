using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using Shouldly;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Sessions.DeleteOthers;

[Collection(nameof(ContainersCollection))]
public sealed class When_DeletingOtherSessions_WithTwoLogins_Should(ContainersFixture fixture)
    : SessionManagementIntegrationTestBase(fixture)
{
    [Fact]
    public async Task KeepCurrentSession()
    {
        var first = await SignInAsync();
        var current = await SignInAsync();
        UseAccessToken(current.AccessToken!);

        var deleted = Track(await HttpClient.DeleteAsync($"{EndpointUrl.Sessions.V1}/others"));
        var rejected = Track(await HttpClient.PostAsJsonAsync(EndpointUrl.Sessions.RefreshV1,
            new RefreshSessionRequest(first.RefreshToken!)));
        var active = Track(await HttpClient.PostAsJsonAsync(EndpointUrl.Sessions.RefreshV1,
            new RefreshSessionRequest(current.RefreshToken!)));

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        rejected.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        active.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
