using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Features.StartGoogleAuthenticationFlow;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.GoogleFlows;

[ApiController]
[ApiVersion("1.0")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route(EndpointUrl.GoogleAuthenticationFlows.Route)]
public sealed class GoogleAuthenticationFlowsController(
    StartGoogleAuthenticationFlowHandler handler,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<GoogleAuthenticationFlowResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<GoogleAuthenticationFlowResponse>> Handle(CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new StartGoogleAuthenticationFlowCommand(ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return Created(
            (string?)null,
            new GoogleAuthenticationFlowResponse(result.FlowToken, result.Nonce, result.ExpiresAtUtc));
    }
}
