using BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;
using Shouldly;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace BLVK.Pickly.Logistics.WebAPI.IntegrationTests.Authentication.Sessions.List;

[Collection(nameof(ContainersCollection))]
public sealed class When_GettingSessions_WithTwoLogins_Should(ContainersFixture fixture)
    : SessionManagementIntegrationTestBase(fixture)
{
    [Fact]
    public async Task ReturnCurrentSessionFirst()
    {
        var first = await SignInAsync();
        var second = await SignInAsync();
        UseAccessToken(second.AccessToken!);

        var response = Track(await HttpClient.GetAsync(EndpointUrl.Sessions.V1));
        var sessions = await response.Content.ReadFromJsonAsync<ActiveSessionResponse[]>();
        var sid = new JwtSecurityTokenHandler().ReadJwtToken(second.AccessToken!).Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid).Value;

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        sessions.ShouldNotBeNull();
        sessions.Length.ShouldBe(2);
        sessions[0].IsCurrent.ShouldBeTrue();
        sessions[0].SessionId.ToString().ShouldBe(sid);
        sessions[1].IsCurrent.ShouldBeFalse();
        sessions[0].City.ShouldBeNull();
        sessions[0].State.ShouldBeNull();
        sessions[0].Country.ShouldBeNull();
        first.AccessToken.ShouldNotBe(second.AccessToken);
    }
}
