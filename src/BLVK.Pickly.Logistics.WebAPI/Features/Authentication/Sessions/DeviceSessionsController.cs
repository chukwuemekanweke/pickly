using Asp.Versioning;
using BLVK.Pickly.Logistics.Application.Authentication.Constants;
using BLVK.Pickly.Logistics.Application.Authentication.Features.DeviceSignIn;
using BLVK.Pickly.Logistics.Application.Authentication.Features.SignIn;
using BLVK.Pickly.Logistics.Domain.Common.Auditing;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure;
using BLVK.Pickly.Logistics.WebAPI.Infrastructure.RateLimiting;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BLVK.Pickly.Logistics.WebAPI.Features.Authentication.Sessions;

[ApiController]
[ApiVersion("1.0")]
[Tags("Sessions")]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
[Route($"{EndpointUrl.Sessions.Route}/device")]
public sealed class DeviceSessionsController(
    DeviceSignInHandler handler,
    IValidator<DeviceSignInRequest> validator,
    ICurrentActor currentActor) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitingPolicyNames.SignInPolicy)]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SignInResponse>> Handle(
        [FromBody] DeviceSignInRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ValidationProblemDetails(validationResult.ToValidationDictionary()));
        }

        var result = await handler.HandleAsync(
            new DeviceSignInCommand(
                request.DeviceIdentifier,
                HttpContext.GetClientIpAddress(),
                Request.Headers.UserAgent.ToString(),
                ActorContext.FromAnonymousActor(currentActor)),
            cancellationToken);

        return Ok(new SignInResponse(
            AuthenticationOutcomes.Authenticated,
            result.Tokens.AccessToken.Value,
            result.Tokens.AccessToken.ExpiresAtUtc,
            result.Tokens.RefreshToken.Value,
            result.Tokens.RefreshToken.ExpiresAtUtc,
            "Bearer"));
    }
}
