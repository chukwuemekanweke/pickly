using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Sessions.Delete;

[Collection(nameof(ContainersCollection))]
public sealed class When_DeletingSession_WithOwnedSession_Should(ContainersFixture fixture)
    : SessionManagementIntegrationTestBase(fixture)
{
    [Fact]
    public async Task PreventRefresh()
    {
        var login = await SignInAsync();
        UseAccessToken(login.AccessToken!);
        var sid = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken!).Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value;

        var deleted = Track(await HttpClient.DeleteAsync($"{EndpointUrl.Sessions.V1}/{sid}"));
        var refreshed = Track(await HttpClient.PostAsJsonAsync(EndpointUrl.Sessions.RefreshV1,
            new RefreshSessionRequest(login.RefreshToken!)));

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        refreshed.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
